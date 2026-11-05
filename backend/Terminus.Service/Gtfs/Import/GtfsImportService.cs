using System.Security.Cryptography;
using System.Text;
using Terminus.Model.Common;
using Terminus.Repository.Common;
using Terminus.Service.Common;

namespace Terminus.Service.Gtfs.Import;

public class GtfsImportService(
    IGtfsImportRepository importRepository,
    ISetupRepository setupRepository,
    ISettingsRepository settingsRepository,
    TimeProvider timeProvider) : IGtfsImportService
{
    public async Task<GtfsImportReport> PreviewAsync(Stream zip, CancellationToken cancellationToken = default)
    {
        var (report, _, _) = await ReadAsync(zip, new GtfsImportOptions(), cancellationToken);
        return report;
    }

    public async Task<GtfsImportReport> ImportAsync(Stream zip, GtfsImportOptions options, CancellationToken cancellationToken = default)
    {
        var (report, feed, variants) = await ReadAsync(zip, options, cancellationToken);
        if (feed is null)
        {
            return report;
        }

        DepartureLinker.Link(feed);
        await importRepository.ReplaceAllAsync(feed, variants, cancellationToken);
        await settingsRepository.MarkSetupCompletedAsync(timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

        return report with { Imported = true };
    }

    private async Task<(GtfsImportReport Report, GtfsFeedData? Feed, IReadOnlyList<IRouteVariantModel> Variants)> ReadAsync(
        Stream zip,
        GtfsImportOptions options,
        CancellationToken cancellationToken)
    {
        var existing = await setupRepository.GetCountsAsync(cancellationToken);
        var builder = new ImportReportBuilder();

        GtfsFeedData? feed = null;
        var counts = new GtfsImportCounts(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, false, 0, 0);

        using (var archive = GtfsArchive.Open(zip, out var error))
        {
            if (archive is null)
            {
                builder.Error(null, error!);
            }
            else
            {
                // Čitanje i provjera su CPU posao nad cijelim feedom; ne blokira dretvu zahtjeva.
                (feed, counts) = await Task.Run(() => GtfsFeedReader.Read(archive, builder), cancellationToken);
            }
        }

        var expired = new GtfsImportExpired(0, 0, 0);
        GtfsImportSeason? season = null;
        GtfsImportSeason? activeSeason = null;
        var derived = new List<DerivedVariant>();
        if (feed is not null)
        {
            var today = Today(feed);
            var expiredCalendars = ExpiredCalendars.Find(feed.Calendars, feed.CalendarDates, today);
            var expiredTrips = feed.Trips.Where(t => expiredCalendars.Contains(t.ServiceCalendarId)).Select(t => t.Id).ToHashSet();
            expired = new GtfsImportExpired(
                expiredCalendars.Count,
                expiredTrips.Count,
                feed.StopTimes.Count(st => expiredTrips.Contains(st.TripId)));

            var active = WithoutExpired(feed, expiredCalendars, expiredTrips);
            season = Summary(FeedSeasonBuilder.Build(feed, null));
            activeSeason = Summary(FeedSeasonBuilder.Build(active, today));

            if (options.SkipExpired)
            {
                feed = active;
            }

            derived = DeriveVariants(feed, options.VariantKeys, expiredTrips);
            feed = WithChosenTrips(feed, derived);
            if (options.CreateSeason)
            {
                feed = WithSeason(feed, FeedSeasonBuilder.Build(feed, options.SkipExpired ? today : null), builder);
            }

            counts = counts with
            {
                Calendars = feed.Calendars.Count,
                CalendarDates = feed.CalendarDates.Count,
                Trips = feed.Trips.Count,
                StopTimes = feed.StopTimes.Count
            };
        }

        var report = new GtfsImportReport(
            Imported: false,
            CanImport: !builder.HasErrors,
            Counts: counts,
            Existing: existing,
            Expired: expired,
            Season: season,
            ActiveSeason: activeSeason,
            Variants: derived.Select(d => d.Summary).ToList(),
            Errors: builder.Errors,
            Warnings: builder.Warnings,
            SkippedFiles: builder.SkippedFiles);

        return (report, feed, derived.Where(d => d.Summary.Included).Select(d => d.Variant).ToList());
    }

    /// <summary>Današnji dan u zoni prijevoznika; svi prijevoznici feeda imaju istu zonu.</summary>
    private DateOnly Today(GtfsFeedData feed)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(feed.Agencies[0].Timezone);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), zone).DateTime);
    }

    private static GtfsFeedData WithoutExpired(GtfsFeedData feed, IReadOnlySet<Guid> calendars, IReadOnlySet<Guid> trips) => feed with
    {
        Calendars = feed.Calendars.Where(c => !calendars.Contains(c.Id)).ToList(),
        CalendarDates = feed.CalendarDates.Where(d => !calendars.Contains(d.ServiceCalendarId)).ToList(),
        Trips = feed.Trips.Where(t => !trips.Contains(t.Id)).ToList(),
        StopTimes = feed.StopTimes.Where(st => !trips.Contains(st.TripId)).ToList()
    };

    private static GtfsImportSeason Summary(FeedSeason season) => new(
        season.Problem,
        season.Season?.Name,
        season.Season?.StartDate,
        season.Season?.EndDate,
        season.DayTypes.Select(d => d.Name).ToList(),
        season.Days.Count);

    private static GtfsFeedData WithSeason(GtfsFeedData feed, FeedSeason season, ImportReportBuilder report)
    {
        if (season.Problem is not null)
        {
            report.Warning(null, $"Sezona nije napravljena: {season.Problem} Kalendari ostaju izvan sezona.");
            return feed;
        }

        return feed with
        {
            Season = season.Season,
            SeasonDays = season.Days,
            Calendars = [.. season.DayTypes, .. season.OtherCalendars],
            CalendarDates = [.. season.CalendarDates, .. season.OtherCalendarDates]
        };
    }

    /// <summary>
    /// Vožnje za upis: one odabranih varijanti, povezane s varijantom. Vožnje bez
    /// kalendara (feed nema calendar.txt ni calendar_dates.txt) se ne upisuju.
    /// </summary>
    private static GtfsFeedData WithChosenTrips(GtfsFeedData feed, IReadOnlyList<DerivedVariant> derived)
    {
        var variantByTrip = derived
            .Where(d => d.Summary.Included)
            .SelectMany(d => d.TripIds.Select(tripId => (TripId: tripId, VariantId: d.Variant.Id)))
            .ToDictionary(x => x.TripId, x => x.VariantId);

        var trips = new List<ITripModel>();
        foreach (var trip in feed.Trips)
        {
            if (trip.ServiceCalendarId != Guid.Empty && variantByTrip.TryGetValue(trip.Id, out var variantId))
            {
                trip.VariantId = variantId;
                trips.Add(trip);
            }
        }

        var kept = trips.Select(t => t.Id).ToHashSet();
        return feed with { Trips = trips, StopTimes = feed.StopTimes.Where(st => kept.Contains(st.TripId)).ToList() };
    }

    /// <summary>Varijante svake linije iz njezinih vožnji; bez odabira se uvoze sve.</summary>
    private static List<DerivedVariant> DeriveVariants(
        GtfsFeedData feed,
        IReadOnlyCollection<string>? variantKeys,
        IReadOnlySet<Guid> expiredTrips)
    {
        var stops = feed.Stops.ToDictionary(s => s.Id);
        var stopTimes = feed.StopTimes.GroupBy(st => st.TripId).ToDictionary(g => g.Key, g => (IReadOnlyList<IStopTimeModel>)g.OrderBy(st => st.StopSequence).ToList());
        var tripsByRoute = feed.Trips
            .Select(t => new TripStopTimes(t, stopTimes.GetValueOrDefault(t.Id) ?? []))
            .ToLookup(t => t.Trip.RouteId);

        var chosen = variantKeys?.ToHashSet(StringComparer.Ordinal);
        var result = new List<DerivedVariant>();
        foreach (var route in feed.Routes)
        {
            var trips = tripsByRoute[route.Id].ToList();
            var variants = RouteVariantBuilder.Derive(route.Id, trips, id => stops.TryGetValue(id, out var stop) ? stop.Name : "?");

            foreach (var (variant, tripIds) in variants)
            {
                var stopCodes = variant.Stops.Select(s => stops[s.StopId].Code);
                var key = Key($"{route.Code}|{variant.DirectionId}|{string.Join('>', stopCodes)}");
                var share = (double)tripIds.Count / Math.Max(1, trips.Count(t => t.Trip.DirectionId == variant.DirectionId));

                result.Add(new DerivedVariant(variant, tripIds, new GtfsImportVariant(
                    key,
                    route.Code,
                    route.ShortName ?? route.LongName,
                    variant.DirectionId,
                    variant.Name,
                    variant.Headsign,
                    variant.Stops.Count,
                    tripIds.Count,
                    tripIds.Count(expiredTrips.Contains),
                    variant.Stops.Sum(s => s.TravelTime + s.DwellTime),
                    share,
                    Main: variant.SortOrder == 0,
                    Included: chosen?.Contains(key) ?? true)));
            }
        }

        return result;
    }

    /// <summary>Kratki stalni ključ (isti feed daje isti ključ i u pregledu i u uvozu).</summary>
    private static string Key(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..16].ToLowerInvariant();

    private sealed record DerivedVariant(IRouteVariantModel Variant, IReadOnlyList<Guid> TripIds, GtfsImportVariant Summary);
}
