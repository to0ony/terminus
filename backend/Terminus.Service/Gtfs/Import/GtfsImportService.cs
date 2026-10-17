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
    /// <summary>Sporedna varijanta se zadano uvozi ako ima barem toliki udio vožnji smjera.</summary>
    public const double MinVariantShare = 0.05;

    public async Task<GtfsImportReport> PreviewAsync(Stream zip, CancellationToken cancellationToken = default)
    {
        var (report, _, _) = await ReadAsync(zip, null, cancellationToken);
        return report;
    }

    public async Task<GtfsImportReport> ImportAsync(Stream zip, IReadOnlyCollection<string>? variantKeys, CancellationToken cancellationToken = default)
    {
        var (report, feed, variants) = await ReadAsync(zip, variantKeys, cancellationToken);
        if (feed is null)
        {
            return report;
        }

        await importRepository.MergeAsync(feed, variants, cancellationToken);
        await settingsRepository.MarkSetupCompletedAsync(timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

        return report with { Imported = true };
    }

    private async Task<(GtfsImportReport Report, GtfsFeedData? Feed, IReadOnlyList<IRouteVariantModel> Variants)> ReadAsync(
        Stream zip,
        IReadOnlyCollection<string>? variantKeys,
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

        var changes = new GtfsImportChanges(0, 0, 0, 0, 0, 0, 0, 0);
        var derived = new List<(IRouteVariantModel Variant, GtfsImportVariant Summary)>();
        if (feed is not null)
        {
            var codes = await importRepository.GetExistingCodesAsync(cancellationToken);
            changes = Changes(feed, codes);
            derived = DeriveVariants(feed, variantKeys);
        }

        var report = new GtfsImportReport(
            Imported: false,
            CanImport: !builder.HasErrors,
            Counts: counts,
            Existing: existing,
            Changes: changes,
            Variants: derived.Select(d => d.Summary).ToList(),
            Errors: builder.Errors,
            Warnings: builder.Warnings,
            SkippedFiles: builder.SkippedFiles);

        return (report, feed, derived.Where(d => d.Summary.Included).Select(d => d.Variant).ToList());
    }

    private static GtfsImportChanges Changes(GtfsFeedData feed, GtfsExistingCodes codes)
    {
        static (int New, int Updated) Count(IEnumerable<string> feedCodes, IReadOnlySet<string> existing)
        {
            var list = feedCodes.ToList();
            var updated = list.Count(existing.Contains);
            return (list.Count - updated, updated);
        }

        var agencies = Count(feed.Agencies.Select(a => a.Code), codes.Agencies);
        var stops = Count(feed.Stops.Select(s => s.Code), codes.Stops);
        var routes = Count(feed.Routes.Select(r => r.Code), codes.Routes);
        var shapes = Count(feed.Shapes.Select(s => s.Code), codes.Shapes);
        return new GtfsImportChanges(
            agencies.New, agencies.Updated, stops.New, stops.Updated, routes.New, routes.Updated, shapes.New, shapes.Updated);
    }

    /// <summary>
    /// Varijante svake linije iz njezinih vožnji. Zadano se uvoze glavne i one s
    /// barem 5 % vožnji smjera; rijetke (npr. vožnje u garažu) korisnik uključuje sam.
    /// </summary>
    private static List<(IRouteVariantModel, GtfsImportVariant)> DeriveVariants(GtfsFeedData feed, IReadOnlyCollection<string>? variantKeys)
    {
        var stops = feed.Stops.ToDictionary(s => s.Id);
        var stopTimes = feed.StopTimes.GroupBy(st => st.TripId).ToDictionary(g => g.Key, g => (IReadOnlyList<IStopTimeModel>)g.OrderBy(st => st.StopSequence).ToList());
        var tripsByRoute = feed.Trips
            .Select(t => new TripStopTimes(t, stopTimes.GetValueOrDefault(t.Id) ?? []))
            .ToLookup(t => t.Trip.RouteId);

        var result = new List<(IRouteVariantModel, GtfsImportVariant)>();
        foreach (var route in feed.Routes)
        {
            var trips = tripsByRoute[route.Id].ToList();
            var variants = RouteVariantBuilder.Derive(route.Id, trips, id => stops.TryGetValue(id, out var stop) ? stop.Name : "?");

            foreach (var (variant, tripIds) in variants)
            {
                var stopCodes = variant.Stops.Select(s => stops[s.StopId].Code);
                var key = Key($"{route.Code}|{variant.DirectionId}|{string.Join('>', stopCodes)}");
                var share = (double)tripIds.Count / Math.Max(1, trips.Count(t => t.Trip.DirectionId == variant.DirectionId));
                var main = variant.SortOrder == 0;
                var included = variantKeys?.Contains(key) ?? (main || share >= MinVariantShare);

                result.Add((variant, new GtfsImportVariant(
                    key,
                    route.Code,
                    route.ShortName ?? route.LongName,
                    variant.DirectionId,
                    variant.Name,
                    variant.Headsign,
                    variant.Stops.Count,
                    tripIds.Count,
                    variant.Stops.Sum(s => s.TravelTime + s.DwellTime),
                    share,
                    main,
                    included)));
            }
        }

        return result;
    }

    /// <summary>Kratki stalni ključ (isti feed daje isti ključ i u pregledu i u uvozu).</summary>
    private static string Key(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..16].ToLowerInvariant();
}
