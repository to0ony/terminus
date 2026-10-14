using System.Globalization;
using System.IO.Compression;
using System.Text;
using Terminus.Common;
using Terminus.Common.Exceptions;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Repository.Common;
using Terminus.Service.Common;

namespace Terminus.Service.Gtfs;

public class GtfsExportService(IGtfsExportRepository repository) : IGtfsExportService
{
    private const int MinStopTimesPerTrip = 2;

    public async Task<GtfsFeedFile> ExportAsync(Guid agencyId, CancellationToken cancellationToken = default)
    {
        var agency = await repository.GetAgencyAsync(agencyId, cancellationToken)
            ?? throw new NotFoundException($"Agencija {agencyId} ne postoji.");

        // DbContext nije thread-safe, pa se upiti izvršavaju jedan za drugim.
        var source = new FeedSource(
            Stops: await repository.GetStopsAsync(agencyId, cancellationToken),
            Routes: await repository.GetRoutesAsync(agencyId, cancellationToken),
            Calendars: await repository.GetServiceCalendarsAsync(agencyId, cancellationToken),
            CalendarDates: await repository.GetCalendarDatesAsync(agencyId, cancellationToken),
            Trips: await repository.GetTripsAsync(agencyId, cancellationToken),
            StopTimes: await repository.GetStopTimesAsync(agencyId, cancellationToken));

        var feed = Select(source);
        Validate(feed);

        var generatedAt = DateTime.UtcNow;
        return new GtfsFeedFile(FileName(agency, generatedAt), Write(agency, feed, generatedAt));
    }

    /// <summary>
    /// Aktivne linije → njihove vožnje → kalendari i stanice koje te vožnje koriste
    /// (uključujući nadređene stanice perona).
    /// </summary>
    private static Feed Select(FeedSource source)
    {
        var routes = source.Routes.Where(r => r.IsActive).ToList();
        var routeIds = routes.Select(r => r.Id).ToHashSet();

        var trips = source.Trips.Where(t => routeIds.Contains(t.RouteId)).ToList();
        var tripIds = trips.Select(t => t.Id).ToHashSet();

        var stopTimesByTrip = source.StopTimes
            .Where(st => tripIds.Contains(st.TripId))
            .GroupBy(st => st.TripId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<IStopTimeModel>)g.ToList());

        var calendarIds = trips.Select(t => t.ServiceCalendarId).ToHashSet();
        var calendars = source.Calendars.Where(c => calendarIds.Contains(c.Id)).ToList();
        var calendarDates = source.CalendarDates.Where(d => calendarIds.Contains(d.ServiceCalendarId)).ToList();

        var allStops = source.Stops.ToDictionary(s => s.Id);
        var usedStopIds = stopTimesByTrip.Values.SelectMany(list => list).Select(st => st.StopId).ToHashSet();

        // GTFS traži da parent_station postoji u feedu.
        var parentIds = usedStopIds
            .Select(id => allStops.GetValueOrDefault(id)?.ParentStationId)
            .OfType<Guid>();

        var exportedStopIds = usedStopIds.Union(parentIds).ToHashSet();
        var stops = source.Stops.Where(s => exportedStopIds.Contains(s.Id)).ToList();

        return new Feed(
            routes,
            trips,
            stopTimesByTrip,
            calendars,
            calendarDates,
            stops,
            allStops,
            routes.ToDictionary(r => r.Id),
            calendars.ToDictionary(c => c.Id));
    }

    /// <summary>Problemi koje pravila pri unosu ne mogu spriječiti jer se podaci s vremenom mijenjaju.</summary>
    private static void Validate(Feed feed)
    {
        var problems = new List<string>();

        if (feed.Trips.Count == 0)
        {
            problems.Add("Feed nema nijednu vožnju na aktivnoj liniji.");
        }

        foreach (var trip in feed.Trips)
        {
            var stopTimes = feed.StopTimesByTrip.GetValueOrDefault(trip.Id) ?? [];

            if (stopTimes.Count < MinStopTimesPerTrip)
            {
                problems.Add(stopTimes.Count == 0
                    ? $"Vožnja '{trip.Code}' nema postavljene stanice i vremena."
                    : $"Vožnja '{trip.Code}' ima samo {stopTimes.Count} stanicu; potrebne su barem {MinStopTimesPerTrip}.");
            }

            var inactiveStops = stopTimes
                .Select(st => feed.AllStops[st.StopId])
                .Where(s => !s.IsActive)
                .DistinctBy(s => s.Id);

            foreach (var stop in inactiveStops)
            {
                problems.Add($"Vožnja '{trip.Code}' koristi neaktivnu stanicu '{stop.Name}' ({stop.Code}).");
            }
        }

        var calendarsWithAddedDates = feed.CalendarDates
            .Where(d => d.ExceptionType == CalendarExceptionType.Added)
            .Select(d => d.ServiceCalendarId)
            .ToHashSet();

        foreach (var calendar in feed.Calendars)
        {
            if (!RunsOnAnyWeekday(calendar) && !calendarsWithAddedDates.Contains(calendar.Id))
            {
                problems.Add($"Kalendar '{calendar.Code}' nikad ne vozi: nema označenih dana u tjednu ni dodanih datuma.");
            }
        }

        if (problems.Count > 0)
        {
            throw new UnprocessableException("GTFS feed se ne može izvesti dok se ne isprave navedeni problemi.", problems);
        }
    }

    private static byte[] Write(IAgencyModel agency, Feed feed, DateTime generatedAt)
    {
        using var buffer = new MemoryStream();

        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            GtfsCsvWriter.WriteEntry(archive, "agency.txt",
                ["agency_name", "agency_url", "agency_timezone", "agency_lang", "agency_phone", "agency_email"],
                [[agency.Name, agency.Url, agency.Timezone, agency.Lang, agency.Phone, agency.Email]]);

            GtfsCsvWriter.WriteEntry(archive, "stops.txt",
                ["stop_id", "stop_name", "stop_desc", "stop_lat", "stop_lon", "location_type", "parent_station", "wheelchair_boarding", "platform_code"],
                feed.Stops.Select(s => (IReadOnlyList<string?>)
                [
                    s.Code,
                    s.Name,
                    s.Description,
                    Number(s.Latitude),
                    Number(s.Longitude),
                    Number((short)s.LocationType),
                    s.ParentStationId is Guid parentId ? feed.AllStops[parentId].Code : null,
                    Number((short)s.WheelchairBoarding),
                    s.PlatformCode
                ]));

            GtfsCsvWriter.WriteEntry(archive, "routes.txt",
                ["route_id", "route_short_name", "route_long_name", "route_desc", "route_type", "route_color", "route_text_color", "route_sort_order"],
                feed.Routes.Select(r => (IReadOnlyList<string?>)
                [
                    r.Code,
                    r.ShortName,
                    r.LongName,
                    r.Description,
                    Number((short)r.RouteType),
                    r.Color,
                    r.TextColor,
                    r.SortOrder is int sortOrder ? Number(sortOrder) : null
                ]));

            GtfsCsvWriter.WriteEntry(archive, "trips.txt",
                ["route_id", "service_id", "trip_id", "trip_headsign", "trip_short_name", "direction_id", "block_id", "wheelchair_accessible", "bikes_allowed"],
                feed.Trips.Select(t => (IReadOnlyList<string?>)
                [
                    feed.RoutesById[t.RouteId].Code,
                    feed.CalendarsById[t.ServiceCalendarId].Code,
                    t.Code,
                    t.Headsign,
                    t.ShortName,
                    t.DirectionId is { } direction ? Number((short)direction) : null,
                    t.BlockId,
                    Number((short)t.WheelchairAccessible),
                    Number((short)t.BikesAllowed)
                ]));

            GtfsCsvWriter.WriteEntry(archive, "stop_times.txt",
                ["trip_id", "arrival_time", "departure_time", "stop_id", "stop_sequence", "stop_headsign", "pickup_type", "drop_off_type", "timepoint"],
                feed.Trips.SelectMany(t => feed.StopTimesByTrip[t.Id].Select(st => (IReadOnlyList<string?>)
                [
                    t.Code,
                    GtfsTime.Format(st.ArrivalTime),
                    GtfsTime.Format(st.DepartureTime),
                    feed.AllStops[st.StopId].Code,
                    Number(st.StopSequence),
                    st.StopHeadsign,
                    Number((short)st.PickupType),
                    Number((short)st.DropOffType),
                    Flag(st.Timepoint)
                ])));

            GtfsCsvWriter.WriteEntry(archive, "calendar.txt",
                ["service_id", "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday", "start_date", "end_date"],
                feed.Calendars.Select(c => (IReadOnlyList<string?>)
                [
                    c.Code,
                    Flag(c.Monday),
                    Flag(c.Tuesday),
                    Flag(c.Wednesday),
                    Flag(c.Thursday),
                    Flag(c.Friday),
                    Flag(c.Saturday),
                    Flag(c.Sunday),
                    GtfsDate.Format(c.StartDate),
                    GtfsDate.Format(c.EndDate)
                ]));

            // Opcionalna datoteka; prazna bi kod validatora izazvala upozorenje.
            if (feed.CalendarDates.Count > 0)
            {
                GtfsCsvWriter.WriteEntry(archive, "calendar_dates.txt",
                    ["service_id", "date", "exception_type"],
                    feed.CalendarDates.Select(d => (IReadOnlyList<string?>)
                    [
                        feed.CalendarsById[d.ServiceCalendarId].Code,
                        GtfsDate.Format(d.Date),
                        Number((short)d.ExceptionType)
                    ]));
            }

            var (feedStart, feedEnd) = FeedPeriod(feed);

            GtfsCsvWriter.WriteEntry(archive, "feed_info.txt",
                ["feed_publisher_name", "feed_publisher_url", "feed_lang", "feed_start_date", "feed_end_date", "feed_version"],
                [[
                    agency.Name,
                    agency.Url,
                    agency.Lang,
                    GtfsDate.Format(feedStart),
                    GtfsDate.Format(feedEnd),
                    generatedAt.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture)
                ]]);
        }

        return buffer.ToArray();
    }

    /// <summary>Od najranijeg do najkasnijeg datuma vožnje (razdoblja kalendara i dodani datumi).</summary>
    private static (DateOnly Start, DateOnly End) FeedPeriod(Feed feed)
    {
        var addedDates = feed.CalendarDates
            .Where(d => d.ExceptionType == CalendarExceptionType.Added)
            .Select(d => d.Date)
            .ToList();

        var starts = feed.Calendars.Select(c => c.StartDate).Concat(addedDates);
        var ends = feed.Calendars.Select(c => c.EndDate).Concat(addedDates);

        return (starts.Min(), ends.Max());
    }

    private static bool RunsOnAnyWeekday(IServiceCalendarModel c) =>
        c.Monday || c.Tuesday || c.Wednesday || c.Thursday || c.Friday || c.Saturday || c.Sunday;

    /// <summary>Uvijek s točkom kao decimalnim znakom (hrvatska kultura koristi zarez).</summary>
    private static string Number(double value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Flag(bool value) => value ? "1" : "0";

    /// <summary>npr. "gtfs-test-prijevoznik-d-o-o-20261014.zip"</summary>
    private static string FileName(IAgencyModel agency, DateTime generatedAt) =>
        $"gtfs-{Slug(agency.Name)}-{generatedAt:yyyyMMdd}.zip";

    private static string Slug(string value)
    {
        // Đ/đ se ne rastavlja normalizacijom kao č, ć, š, ž.
        var normalized = value.Replace('Đ', 'D').Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var slug = new StringBuilder();

        foreach (var c in normalized)
        {
            if (char.IsAsciiLetterOrDigit(c))
            {
                slug.Append(char.ToLowerInvariant(c));
            }
            else if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark
                     && slug.Length > 0 && slug[^1] != '-')
            {
                slug.Append('-');
            }
        }

        var result = slug.ToString().Trim('-');
        return result.Length > 0 ? result : "agencija";
    }

    private sealed record FeedSource(
        IReadOnlyList<IStopModel> Stops,
        IReadOnlyList<IRouteModel> Routes,
        IReadOnlyList<IServiceCalendarModel> Calendars,
        IReadOnlyList<ICalendarDateModel> CalendarDates,
        IReadOnlyList<ITripModel> Trips,
        IReadOnlyList<IStopTimeModel> StopTimes);

    private sealed record Feed(
        IReadOnlyList<IRouteModel> Routes,
        IReadOnlyList<ITripModel> Trips,
        IReadOnlyDictionary<Guid, IReadOnlyList<IStopTimeModel>> StopTimesByTrip,
        IReadOnlyList<IServiceCalendarModel> Calendars,
        IReadOnlyList<ICalendarDateModel> CalendarDates,
        IReadOnlyList<IStopModel> Stops,
        IReadOnlyDictionary<Guid, IStopModel> AllStops,
        IReadOnlyDictionary<Guid, IRouteModel> RoutesById,
        IReadOnlyDictionary<Guid, IServiceCalendarModel> CalendarsById);
}
