using System.Globalization;
using System.IO.Compression;
using Terminus.Common;
using Terminus.Common.Exceptions;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Repository.Common;
using Terminus.Service.Common;

namespace Terminus.Service.Gtfs;

public class GtfsExportService(IGtfsExportRepository repository, TimeProvider timeProvider) : IGtfsExportService
{
    private const int MinStopTimesPerTrip = 2;

    private const string NotReady = "GTFS feed se ne može izvesti dok se ne isprave navedeni problemi.";

    public async Task<GtfsFeedFile> ExportAsync(CancellationToken cancellationToken = default)
    {
        var source = await LoadAsync(cancellationToken);
        var (feed, problems) = Check(source);
        if (problems.Count > 0)
        {
            throw new UnprocessableException(NotReady, problems);
        }

        // Bez unesenog feed_info izdavač je prvi uneseni prijevoznik.
        var publisher = SelectPublisher(source);

        var generatedAt = timeProvider.GetUtcNow().UtcDateTime;
        return new GtfsFeedFile(FileName(publisher.Name, generatedAt), Write(publisher, feed!, generatedAt));
    }

    public async Task<IReadOnlyList<string>> CheckAsync(CancellationToken cancellationToken = default) =>
        Check(await LoadAsync(cancellationToken)).Problems;

    private async Task<FeedSource> LoadAsync(CancellationToken cancellationToken) =>
        // DbContext nije thread-safe, pa se upiti izvršavaju jedan za drugim.
        new(
            Agencies: await repository.GetAgenciesAsync(cancellationToken),
            Stops: await repository.GetStopsAsync(cancellationToken),
            Routes: await repository.GetRoutesAsync(cancellationToken),
            Calendars: await repository.GetServiceCalendarsAsync(cancellationToken),
            CalendarDates: await repository.GetCalendarDatesAsync(cancellationToken),
            Trips: await repository.GetTripsAsync(cancellationToken),
            StopTimes: await repository.GetStopTimesAsync(cancellationToken),
            Shapes: await repository.GetShapesAsync(cancellationToken),
            Fares: await repository.GetFaresAsync(cancellationToken),
            FeedInfo: await repository.GetFeedInfoAsync(cancellationToken));

    /// <summary>Odabrani podaci feeda i problemi zbog kojih se ne bi izvezao (bez prijevoznika feed je null).</summary>
    private static (Feed? Feed, IReadOnlyList<string> Problems) Check(FeedSource source)
    {
        if (source.Agencies.Count == 0)
        {
            return (null, ["Nije unesen nijedan prijevoznik."]);
        }

        var feed = Select(source);
        return (feed, Validate(feed));
    }

    /// <summary>
    /// Aktivne linije → njihovi prijevoznici i vožnje → kalendari, trase i stanice
    /// koje te vožnje koriste, s nadređenim stanicama i njihovim ulazima, čvorovima
    /// i mjestima ukrcavanja → cijene koje se odnose na izvezene prijevoznike i linije.
    /// </summary>
    private static Feed Select(FeedSource source)
    {
        var routes = source.Routes.Where(r => r.IsActive).ToList();
        var routeIds = routes.Select(r => r.Id).ToHashSet();

        var agencyIds = routes.Select(r => r.AgencyId).ToHashSet();
        var agencies = source.Agencies.Where(a => agencyIds.Contains(a.Id)).ToList();

        var trips = source.Trips.Where(t => routeIds.Contains(t.RouteId)).ToList();
        var tripIds = trips.Select(t => t.Id).ToHashSet();

        // Vožnje preskaču neaktivne stanice; ostale stanice zadržavaju svoja vremena.
        var inactiveStopIds = source.Stops.Where(s => !s.IsActive).Select(s => s.Id).ToHashSet();
        var stopTimesByTrip = source.StopTimes
            .Where(st => tripIds.Contains(st.TripId) && !inactiveStopIds.Contains(st.StopId))
            .GroupBy(st => st.TripId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<IStopTimeModel>)g.ToList());

        var calendarIds = trips.Select(t => t.ServiceCalendarId).ToHashSet();
        var calendars = source.Calendars.Where(c => calendarIds.Contains(c.Id)).ToList();
        var calendarDates = source.CalendarDates.Where(d => calendarIds.Contains(d.ServiceCalendarId)).ToList();

        var shapeIds = trips.Select(t => t.ShapeId).OfType<Guid>().ToHashSet();
        var shapes = source.Shapes.Where(s => shapeIds.Contains(s.Id)).ToList();

        var stops = SelectStops(source.Stops, stopTimesByTrip.Values.SelectMany(list => list).Select(st => st.StopId));
        var fares = SelectFares(source.Fares, agencyIds, routeIds);

        return new Feed(
            agencies,
            routes,
            trips,
            stopTimesByTrip,
            calendars,
            calendarDates,
            stops,
            shapes,
            fares,
            source.Stops.ToDictionary(s => s.Id),
            source.Agencies.ToDictionary(a => a.Id),
            source.Routes.ToDictionary(r => r.Id),
            calendars.ToDictionary(c => c.Id),
            shapes.ToDictionary(s => s.Id));
    }

    private static List<IStopModel> SelectStops(IReadOnlyList<IStopModel> allStops, IEnumerable<Guid> usedStopIds)
    {
        var byId = allStops.ToDictionary(s => s.Id);
        var exported = usedStopIds.ToHashSet();

        // GTFS traži da parent_station postoji u feedu.
        var parents = exported
            .Select(id => byId.GetValueOrDefault(id)?.ParentStationId)
            .OfType<Guid>()
            .ToList();
        exported.UnionWith(parents);

        // Aktivni ulazi i čvorovi izvezenih stanica te mjesta ukrcavanja izvezenih perona.
        var children = allStops
            .Where(s => s.IsActive
                && s.LocationType is LocationType.Entrance or LocationType.GenericNode or LocationType.BoardingArea
                && s.ParentStationId is Guid parentId
                && exported.Contains(parentId))
            .Select(s => s.Id)
            .ToList();
        exported.UnionWith(children);

        return allStops.Where(s => exported.Contains(s.Id)).ToList();
    }

    /// <summary>
    /// Cijene bez prijevoznika ili s izvezenim prijevoznikom. Pravila za neizvezene
    /// linije se izostavljaju; cijena kojoj tako ne ostane nijedno pravilo izostavlja
    /// se cijela, jer bi bez pravila vrijedila za sve vožnje.
    /// </summary>
    private static List<ExportedFare> SelectFares(IReadOnlyList<IFareModel> fares, IReadOnlySet<Guid> agencyIds, IReadOnlySet<Guid> routeIds)
    {
        var result = new List<ExportedFare>();

        foreach (var fare in fares)
        {
            if (fare.AgencyId is Guid agencyId && !agencyIds.Contains(agencyId))
            {
                continue;
            }

            var rules = fare.Rules
                .Where(r => r.RouteId is not Guid routeId || routeIds.Contains(routeId))
                .ToList();

            if (fare.Rules.Count > 0 && rules.Count == 0)
            {
                continue;
            }

            result.Add(new ExportedFare(fare, rules));
        }

        return result;
    }

    /// <summary>Problemi koje pravila pri unosu ne mogu spriječiti jer se podaci s vremenom mijenjaju.</summary>
    private static List<string> Validate(Feed feed)
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

        ValidateFares(feed, problems);
        return problems;
    }

    private static void ValidateFares(Feed feed, List<string> problems)
    {
        var zones = feed.Stops.Select(s => s.ZoneId).OfType<string>().ToHashSet(StringComparer.Ordinal);

        foreach (var (fare, rules) in feed.Fares)
        {
            // GTFS: agency_id u fare_attributes obavezan je čim feed ima više prijevoznika.
            if (fare.AgencyId is null && feed.Agencies.Count > 1)
            {
                problems.Add($"Cijena '{fare.Code}' nema prijevoznika, a feed ih ima više.");
            }

            var unknownZones = rules
                .SelectMany(r => new[] { r.OriginId, r.DestinationId, r.ContainsId })
                .OfType<string>()
                .Where(zone => !zones.Contains(zone))
                .Distinct(StringComparer.Ordinal);

            foreach (var zone in unknownZones)
            {
                problems.Add($"Cijena '{fare.Code}' koristi zonu '{zone}' koju nema nijedna izvezena stanica.");
            }
        }
    }

    private static byte[] Write(Publisher publisher, Feed feed, DateTime generatedAt)
    {
        using var buffer = new MemoryStream();

        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            // agency_id je obavezan čim feed ima više prijevoznika, pa se uvijek piše.
            GtfsCsvWriter.WriteEntry(archive, "agency.txt",
                ["agency_id", "agency_name", "agency_url", "agency_timezone", "agency_lang", "agency_phone", "agency_fare_url", "agency_email"],
                feed.Agencies.Select(a => (IReadOnlyList<string?>)
                [
                    a.Code,
                    a.Name,
                    a.Url,
                    a.Timezone,
                    a.Lang,
                    a.Phone,
                    a.FareUrl,
                    a.Email
                ]));

            GtfsCsvWriter.WriteEntry(archive, "stops.txt",
                ["stop_id", "stop_code", "stop_name", "tts_stop_name", "stop_desc", "stop_lat", "stop_lon", "zone_id", "stop_url",
                 "location_type", "parent_station", "stop_timezone", "wheelchair_boarding", "platform_code"],
                feed.Stops.Select(s => (IReadOnlyList<string?>)
                [
                    s.Code,
                    s.PublicCode,
                    s.Name,
                    s.TtsName,
                    s.Description,
                    Number(s.Latitude),
                    Number(s.Longitude),
                    s.ZoneId,
                    s.Url,
                    Number((short)s.LocationType),
                    s.ParentStationId is Guid parentId ? feed.AllStops[parentId].Code : null,
                    s.Timezone,
                    Number((short)s.WheelchairBoarding),
                    s.PlatformCode
                ]));

            GtfsCsvWriter.WriteEntry(archive, "routes.txt",
                ["route_id", "agency_id", "route_short_name", "route_long_name", "route_desc", "route_type", "route_url",
                 "route_color", "route_text_color", "route_sort_order", "continuous_pickup", "continuous_drop_off", "network_id"],
                feed.Routes.Select(r => (IReadOnlyList<string?>)
                [
                    r.Code,
                    feed.AgenciesById[r.AgencyId].Code,
                    r.ShortName,
                    r.LongName,
                    r.Description,
                    Number((short)r.RouteType),
                    r.Url,
                    r.Color,
                    r.TextColor,
                    Number(r.SortOrder),
                    Number((short?)r.ContinuousPickup),
                    Number((short?)r.ContinuousDropOff),
                    r.NetworkId
                ]));

            GtfsCsvWriter.WriteEntry(archive, "trips.txt",
                ["route_id", "service_id", "trip_id", "trip_headsign", "trip_short_name", "direction_id", "block_id", "shape_id",
                 "wheelchair_accessible", "bikes_allowed"],
                feed.Trips.Select(t => (IReadOnlyList<string?>)
                [
                    feed.RoutesById[t.RouteId].Code,
                    feed.CalendarsById[t.ServiceCalendarId].Code,
                    t.Code,
                    t.Headsign,
                    t.ShortName,
                    Number((short?)t.DirectionId),
                    t.BlockId,
                    t.ShapeId is Guid shapeId ? feed.ShapesById[shapeId].Code : null,
                    Number((short)t.WheelchairAccessible),
                    Number((short)t.BikesAllowed)
                ]));

            GtfsCsvWriter.WriteEntry(archive, "stop_times.txt",
                ["trip_id", "arrival_time", "departure_time", "stop_id", "stop_sequence", "stop_headsign", "pickup_type", "drop_off_type",
                 "continuous_pickup", "continuous_drop_off", "shape_dist_traveled", "timepoint"],
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
                    Number((short?)st.ContinuousPickup),
                    Number((short?)st.ContinuousDropOff),
                    Number(st.ShapeDistTraveled),
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

            // Neobavezne datoteke pišu se samo kad imaju retke; prazne bi kod validatora izazvale upozorenje.
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

            if (feed.Shapes.Count > 0)
            {
                GtfsCsvWriter.WriteEntry(archive, "shapes.txt",
                    ["shape_id", "shape_pt_lat", "shape_pt_lon", "shape_pt_sequence", "shape_dist_traveled"],
                    feed.Shapes.SelectMany(s => s.Points.Select(p => (IReadOnlyList<string?>)
                    [
                        s.Code,
                        Number(p.Latitude),
                        Number(p.Longitude),
                        Number(p.Sequence),
                        Number(p.DistTraveled)
                    ])));
            }

            if (feed.Fares.Count > 0)
            {
                GtfsCsvWriter.WriteEntry(archive, "fare_attributes.txt",
                    ["fare_id", "price", "currency_type", "payment_method", "transfers", "agency_id", "transfer_duration"],
                    feed.Fares.Select(f => (IReadOnlyList<string?>)
                    [
                        f.Fare.Code,
                        // Bez nepotrebnih nula iz numeric(12,4): 2.4000 se piše kao 2.4.
                        f.Fare.Price.ToString("0.####", CultureInfo.InvariantCulture),
                        f.Fare.CurrencyType,
                        Number((short)f.Fare.PaymentMethod),
                        // Prazno polje znači neograničena presjedanja, ali je stupac obavezan.
                        Number((short?)f.Fare.Transfers),
                        f.Fare.AgencyId is Guid agencyId ? feed.AgenciesById[agencyId].Code : null,
                        Number(f.Fare.TransferDuration)
                    ]));

                var rules = feed.Fares.SelectMany(f => f.Rules.Select(r => (f.Fare, Rule: r))).ToList();
                if (rules.Count > 0)
                {
                    GtfsCsvWriter.WriteEntry(archive, "fare_rules.txt",
                        ["fare_id", "route_id", "origin_id", "destination_id", "contains_id"],
                        rules.Select(x => (IReadOnlyList<string?>)
                        [
                            x.Fare.Code,
                            x.Rule.RouteId is Guid routeId ? feed.RoutesById[routeId].Code : null,
                            x.Rule.OriginId,
                            x.Rule.DestinationId,
                            x.Rule.ContainsId
                        ]));
                }
            }

            var (feedStart, feedEnd) = FeedPeriod(feed);

            GtfsCsvWriter.WriteEntry(archive, "feed_info.txt",
                ["feed_publisher_name", "feed_publisher_url", "feed_lang", "default_lang", "feed_start_date", "feed_end_date",
                 "feed_version", "feed_contact_email", "feed_contact_url"],
                [[
                    publisher.Name,
                    publisher.Url,
                    publisher.Lang,
                    publisher.DefaultLang,
                    GtfsDate.Format(feedStart),
                    GtfsDate.Format(feedEnd),
                    generatedAt.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture),
                    publisher.ContactEmail,
                    publisher.ContactUrl
                ]]);
        }

        return buffer.ToArray();
    }

    private static Publisher SelectPublisher(FeedSource source) => source.FeedInfo is { } info
        ? new Publisher(info.PublisherName, info.PublisherUrl, info.Lang, info.DefaultLang, info.ContactEmail, info.ContactUrl)
        : new Publisher(source.Agencies[0].Name, source.Agencies[0].Url, source.Agencies[0].Lang, null, source.Agencies[0].Email, null);

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
    private static string? Number(double? value) => value?.ToString(CultureInfo.InvariantCulture);

    private static string? Number(int? value) => value?.ToString(CultureInfo.InvariantCulture);

    private static string Flag(bool value) => value ? "1" : "0";

    /// <summary>npr. "gtfs-test-prijevoznik-d-o-o-20261014.zip"</summary>
    private static string FileName(string publisherName, DateTime generatedAt) =>
        $"gtfs-{Slug.From(publisherName, fallback: "agencija")}-{generatedAt:yyyyMMdd}.zip";

    private sealed record FeedSource(
        IReadOnlyList<IAgencyModel> Agencies,
        IReadOnlyList<IStopModel> Stops,
        IReadOnlyList<IRouteModel> Routes,
        IReadOnlyList<IServiceCalendarModel> Calendars,
        IReadOnlyList<ICalendarDateModel> CalendarDates,
        IReadOnlyList<ITripModel> Trips,
        IReadOnlyList<IStopTimeModel> StopTimes,
        IReadOnlyList<IShapeModel> Shapes,
        IReadOnlyList<IFareModel> Fares,
        IFeedInfoModel? FeedInfo);

    private sealed record Feed(
        IReadOnlyList<IAgencyModel> Agencies,
        IReadOnlyList<IRouteModel> Routes,
        IReadOnlyList<ITripModel> Trips,
        IReadOnlyDictionary<Guid, IReadOnlyList<IStopTimeModel>> StopTimesByTrip,
        IReadOnlyList<IServiceCalendarModel> Calendars,
        IReadOnlyList<ICalendarDateModel> CalendarDates,
        IReadOnlyList<IStopModel> Stops,
        IReadOnlyList<IShapeModel> Shapes,
        IReadOnlyList<ExportedFare> Fares,
        IReadOnlyDictionary<Guid, IStopModel> AllStops,
        IReadOnlyDictionary<Guid, IAgencyModel> AgenciesById,
        IReadOnlyDictionary<Guid, IRouteModel> RoutesById,
        IReadOnlyDictionary<Guid, IServiceCalendarModel> CalendarsById,
        IReadOnlyDictionary<Guid, IShapeModel> ShapesById);

    private sealed record ExportedFare(IFareModel Fare, IReadOnlyList<IFareRuleModel> Rules);

    private sealed record Publisher(string Name, string Url, string Lang, string? DefaultLang, string? ContactEmail, string? ContactUrl);
}
