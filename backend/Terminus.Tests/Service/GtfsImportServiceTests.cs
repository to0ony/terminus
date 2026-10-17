using System.IO.Compression;
using System.Text;
using Terminus.Common;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Service.Common;
using Terminus.Service.Gtfs;
using Terminus.Service.Gtfs.Import;
using Terminus.Tests.Fakes;

namespace Terminus.Tests.Service;

public class GtfsImportServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeGtfsImportRepository _repository = new();
    private readonly FakeSettingsRepository _settings = new();
    private readonly FakeSetupRepository _setup = new();

    /// <summary>Mali ispravan feed; testovi mijenjaju ili dodaju datoteke.</summary>
    private readonly Dictionary<string, string> _files = new()
    {
        ["agency.txt"] = """
            agency_id,agency_name,agency_url,agency_timezone,agency_lang
            GPP,GPP Osijek,https://www.gpp.hr,Europe/Zagreb,hr
            """,
        ["stops.txt"] = """
            stop_id,stop_code,stop_name,stop_lat,stop_lon,location_type,parent_station,platform_code
            AK,,Autobusni kolodvor,45.5500,18.6900,1,,
            AK-1,101,Autobusni kolodvor,45.5501,18.6901,0,AK,1
            B,102,Trg,45.5600,18.7000,0,,
            C,103,Bolnica,45.5700,18.7200,0,,
            """,
        ["routes.txt"] = """
            route_id,agency_id,route_short_name,route_long_name,route_type,route_color
            L1,GPP,1,Kolodvor - Bolnica,3,1565c0
            """,
        ["trips.txt"] = """
            route_id,service_id,trip_id,trip_headsign,direction_id
            L1,RD,T1,Bolnica,0
            """,
        ["calendar.txt"] = """
            service_id,monday,tuesday,wednesday,thursday,friday,saturday,sunday,start_date,end_date
            RD,1,1,1,1,1,0,0,20260101,20261231
            """,
        ["stop_times.txt"] = """
            trip_id,arrival_time,departure_time,stop_id,stop_sequence,shape_dist_traveled
            T1,06:00:00,06:00:00,AK-1,1,0
            T1,,,B,2,5
            T1,06:20:00,06:20:00,C,3,20
            """
    };

    private GtfsImportService Service => new(_repository, _setup, _settings, new FixedTimeProvider(Now));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task PreviewAsync_ValidFeed_ReportsCountsWithoutWriting()
    {
        _setup.Counts = new TimetableCounts(1, 10, 2, 50);

        var report = await Service.PreviewAsync(Zip(_files), Ct);

        Assert.True(report.CanImport, Describe(report));
        Assert.False(report.Imported);
        Assert.Equal(new GtfsImportCounts(1, 4, 1, 1, 0, 1, 3, 0, 0, 0, 0, false, 0, 1), report.Counts);
        Assert.Equal(new TimetableCounts(1, 10, 2, 50), report.Existing);
        Assert.Null(_repository.Feed);
        Assert.Null(_settings.Settings?.SetupCompletedAt);
    }

    [Fact]
    public async Task ImportAsync_ValidFeed_WritesDataAndCompletesSetup()
    {
        var report = await Service.ImportAsync(Zip(_files), null, Ct);

        Assert.True(report.Imported, Describe(report));
        Assert.Equal(Now.UtcDateTime, _settings.Settings?.SetupCompletedAt);

        var feed = _repository.Feed!;
        var agency = Assert.Single(feed.Agencies);
        Assert.Equal("GPP", agency.Code);

        var station = feed.Stops.Single(s => s.Code == "AK");
        var platform = feed.Stops.Single(s => s.Code == "AK-1");
        Assert.Equal(LocationType.Station, station.LocationType);
        Assert.Equal(station.Id, platform.ParentStationId);
        Assert.Equal("101", platform.PublicCode);

        var route = Assert.Single(feed.Routes);
        Assert.Equal(agency.Id, route.AgencyId);
        Assert.Equal("1565C0", route.Color);

        var calendar = Assert.Single(feed.Calendars);
        Assert.Equal("Radni dan", calendar.Name);

        var trip = Assert.Single(feed.Trips);
        Assert.Equal((route.Id, calendar.Id, TripDirection.Outbound), (trip.RouteId, trip.ServiceCalendarId, trip.DirectionId));
        Assert.All(feed.StopTimes, st => Assert.Equal(trip.Id, st.TripId));
        Assert.Equal([1, 2, 3], feed.StopTimes.Select(st => st.StopSequence));
    }

    [Fact]
    public async Task ImportAsync_MissingTime_IsInterpolatedByShapeDistance()
    {
        await Service.ImportAsync(Zip(_files), null, Ct);

        // 5 od 20 m puta: četvrtina od 20 minuta.
        var middle = _repository.Feed!.StopTimes[1];
        Assert.Equal(Time("06:05:00"), middle.ArrivalTime);
        Assert.Equal(Time("06:05:00"), middle.DepartureTime);
        Assert.False(middle.Timepoint);
        Assert.True(_repository.Feed.StopTimes[0].Timepoint);
    }

    [Fact]
    public async Task ImportAsync_MissingTimeWithoutDistance_UsesStopDistance()
    {
        // B je bliže C nego AK-1, pa vrijeme na B mora biti bliže 06:20 nego polovina puta.
        _files["stops.txt"] = """
            stop_id,stop_name,stop_lat,stop_lon
            AK-1,Kolodvor,45.5000,18.7000
            B,Trg,45.5900,18.7000
            C,Bolnica,45.6000,18.7000
            """;
        _files["stop_times.txt"] = """
            trip_id,arrival_time,departure_time,stop_id,stop_sequence
            T1,06:00:00,06:00:00,AK-1,1
            T1,,,B,2
            T1,06:20:00,06:20:00,C,3
            """;

        await Service.ImportAsync(Zip(_files), null, Ct);

        Assert.Equal(Time("06:18:00"), _repository.Feed!.StopTimes[1].ArrivalTime);
    }

    [Fact]
    public async Task ImportAsync_Frequencies_AreExpandedIntoTrips()
    {
        _files["frequencies.txt"] = """
            trip_id,start_time,end_time,headway_secs
            T1,07:00:00,08:00:00,1200
            """;

        var report = await Service.ImportAsync(Zip(_files), null, Ct);

        Assert.True(report.Imported, Describe(report));
        Assert.Equal(3, report.Counts.TripsFromFrequencies);

        var feed = _repository.Feed!;
        Assert.Equal(["T1-070000", "T1-072000", "T1-074000"], feed.Trips.Select(t => t.Code));

        var second = feed.Trips[1];
        var times = feed.StopTimes.Where(st => st.TripId == second.Id).Select(st => st.DepartureTime);
        Assert.Equal([Time("07:20:00"), Time("07:25:00"), Time("07:40:00")], times);
    }

    [Fact]
    public async Task ImportAsync_ServiceOnlyInCalendarDates_GetsCalendarWithoutWeekdays()
    {
        _files.Remove("calendar.txt");
        _files["calendar_dates.txt"] = """
            service_id,date,exception_type
            RD,20261224,1
            RD,20261231,1
            """;

        var report = await Service.ImportAsync(Zip(_files), null, Ct);

        Assert.True(report.Imported, Describe(report));
        var calendar = Assert.Single(_repository.Feed!.Calendars);
        Assert.False(calendar.Monday || calendar.Saturday || calendar.Sunday);
        Assert.Equal((new DateOnly(2026, 12, 24), new DateOnly(2026, 12, 31)), (calendar.StartDate, calendar.EndDate));
        Assert.Equal("Posebni datumi: 24.12., 31.12.", calendar.Name);
        Assert.Equal(2, _repository.Feed.CalendarDates.Count);
    }

    [Theory]
    [InlineData("1111100", "Radni dan")]
    [InlineData("0000010", "Subota")]
    [InlineData("0000001", "Nedjelja")]
    [InlineData("0000011", "Subota i nedjelja")]
    [InlineData("1111110", "Radni dan i subota")]
    [InlineData("1111111", "Svaki dan")]
    [InlineData("1010100", "Pon, Sri, Pet")]
    public void CalendarName_FromWeekdays(string days, string expected)
    {
        var calendar = new ServiceCalendarModel
        {
            Monday = days[0] == '1', Tuesday = days[1] == '1', Wednesday = days[2] == '1', Thursday = days[3] == '1',
            Friday = days[4] == '1', Saturday = days[5] == '1', Sunday = days[6] == '1'
        };

        Assert.Equal(expected, GtfsFeedReader.CalendarName(calendar, []));
    }

    [Theory]
    [InlineData("2026-12-07,2026-12-08,2026-12-09,2026-12-10,2026-12-11,2026-12-14", "Radni dan")]
    [InlineData("2026-12-06,2026-12-13,2026-12-20,2026-12-25", "Nedjelja i praznici")]
    [InlineData("2026-12-05,2026-12-12,2026-12-19,2026-12-26", "Subota")]
    [InlineData("2026-12-31,2026-12-24", "Posebni datumi: 24.12., 31.12.")]
    public void CalendarName_WithoutWeekdays_FromAddedDates(string dates, string expected)
    {
        var added = dates.Split(',').Select(DateOnly.Parse).ToList();

        Assert.Equal(expected, GtfsFeedReader.CalendarName(new ServiceCalendarModel(), added));
    }

    [Fact]
    public async Task ImportAsync_MissingRequiredFile_IsRejectedWithoutWriting()
    {
        _files.Remove("routes.txt");

        var report = await Service.ImportAsync(Zip(_files), null, Ct);

        Assert.False(report.CanImport);
        Assert.False(report.Imported);
        Assert.Contains(report.Errors, e => e.File == "routes.txt");
        Assert.Null(_repository.Feed);
        Assert.Null(_settings.Settings?.SetupCompletedAt);
    }

    [Fact]
    public async Task PreviewAsync_DuplicateStopId_IsError()
    {
        _files["stops.txt"] += "\nB,Opet trg,45.5600,18.7000,0,,";

        var report = await Service.PreviewAsync(Zip(_files), Ct);

        var error = Assert.Single(report.Errors);
        Assert.Equal(("stops.txt", "Dvostruki stop_id."), (error.File, error.Message));
    }

    [Fact]
    public async Task PreviewAsync_TripThroughUnknownStop_IsSkippedWithWarning()
    {
        _files["trips.txt"] += "\nL1,RD,T2,Bolnica,0";
        _files["stop_times.txt"] += "\nT2,07:00:00,07:00:00,AK-1,1,\nT2,07:10:00,07:10:00,NEMA,2,";

        var report = await Service.PreviewAsync(Zip(_files), Ct);

        Assert.True(report.CanImport, Describe(report));
        Assert.Equal(1, report.Counts.Trips);
        var warning = Assert.Single(report.Warnings);
        Assert.Equal("stop_times.txt", warning.File);
        Assert.Contains("nepostojeću", warning.Message);
    }

    [Fact]
    public async Task PreviewAsync_NoTripLeft_IsWarning()
    {
        _files["stop_times.txt"] = """
            trip_id,arrival_time,departure_time,stop_id,stop_sequence
            T1,06:00:00,06:00:00,AK-1,1
            T1,05:00:00,05:00:00,C,2
            """;

        var report = await Service.PreviewAsync(Zip(_files), Ct);

        Assert.True(report.CanImport, Describe(report));
        Assert.Contains(report.Warnings, w => w.Message.Contains("unatrag"));
        Assert.Contains(report.Warnings, w => w.Message.Contains("nijedna vožnja"));
        Assert.Empty(report.Variants);
    }

    [Fact]
    public async Task PreviewAsync_DerivesVariantsWithRunningTimes()
    {
        _files["trips.txt"] += "\nL1,RD,T2,Bolnica,0\nL1,RD,T3,Trg,0";
        _files["stop_times.txt"] += """

            T2,07:00:00,07:00:00,AK-1,1,0
            T2,07:04:00,07:04:00,B,2,5
            T2,07:20:00,07:20:00,C,3,20
            T3,08:00:00,08:00:00,AK-1,1,0
            T3,08:05:00,08:05:00,B,2,5
            """;

        var report = await Service.PreviewAsync(Zip(_files), Ct);

        Assert.True(report.CanImport, Describe(report));
        Assert.Equal(2, report.Variants.Count);
        var main = report.Variants[0];
        Assert.Equal(("Autobusni kolodvor → Bolnica", 3, 2, true, true), (main.Name, main.StopCount, main.TripCount, main.Main, main.Included));
        Assert.Equal(20 * 60, main.Duration);
        Assert.Equal("1", main.RouteName);
        // Treća vožnja je trećina smjera, pa se i ona uvozi zadano.
        Assert.True(report.Variants[1].Included);
        Assert.Null(_repository.Feed);
    }

    [Fact]
    public async Task ImportAsync_ChosenVariantsOnly()
    {
        _files["trips.txt"] += "\nL1,RD,T3,Trg,0";
        _files["stop_times.txt"] += """

            T3,08:00:00,08:00:00,AK-1,1,0
            T3,08:05:00,08:05:00,B,2,5
            """;
        var preview = await Service.PreviewAsync(Zip(_files), Ct);
        var shortKey = preview.Variants.Single(v => !v.Main).Key;

        var report = await Service.ImportAsync(Zip(_files), [shortKey], Ct);

        Assert.True(report.Imported, Describe(report));
        var variant = Assert.Single(_repository.Variants);
        Assert.Equal(2, variant.Stops.Count);
        Assert.Equal([false, true], report.Variants.Select(v => v.Included));
    }

    [Fact]
    public async Task ImportAsync_FeedWithoutTripsAndCalendars_ImportsTheBase()
    {
        _files.Remove("trips.txt");
        _files.Remove("stop_times.txt");
        _files.Remove("calendar.txt");

        var report = await Service.ImportAsync(Zip(_files), null, Ct);

        Assert.True(report.Imported, Describe(report));
        Assert.Equal(4, _repository.Feed!.Stops.Count);
        Assert.Empty(_repository.Variants);
        Assert.Contains(report.Warnings, w => w.Message.Contains("varijante"));
    }

    [Fact]
    public async Task ImportAsync_WithoutCalendars_StillDerivesVariants()
    {
        _files.Remove("calendar.txt");

        var report = await Service.ImportAsync(Zip(_files), null, Ct);

        Assert.True(report.Imported, Describe(report));
        Assert.Single(_repository.Variants);
    }

    [Fact]
    public async Task PreviewAsync_ExistingCodes_AreUpdates()
    {
        _repository.Existing = new(
            new HashSet<string> { "GPP" }, new HashSet<string> { "AK", "B" }, new HashSet<string>(), new HashSet<string>());

        var report = await Service.PreviewAsync(Zip(_files), Ct);

        Assert.Equal(new GtfsImportChanges(0, 1, 2, 2, 1, 0, 0, 0), report.Changes);
    }

    [Fact]
    public async Task PreviewAsync_DifferentAgencyTimezones_IsError()
    {
        _files["agency.txt"] += "\nDRUGI,Drugi,https://example.com,Europe/London,en";

        var report = await Service.PreviewAsync(Zip(_files), Ct);

        Assert.Contains(report.Errors, e => e.Message.Contains("različite vremenske zone"));
    }

    [Fact]
    public async Task PreviewAsync_UnsupportedFiles_AreListedAsSkipped()
    {
        _files["transfers.txt"] = "from_stop_id,to_stop_id,transfer_type\nAK-1,B,0";
        _files["fare_products.txt"] = "fare_product_id,amount,currency\nP,1,EUR";

        var report = await Service.PreviewAsync(Zip(_files), Ct);

        Assert.True(report.CanImport, Describe(report));
        Assert.Equal(["fare_products.txt", "transfers.txt"], report.SkippedFiles.Select(f => f.File));
    }

    [Fact]
    public async Task PreviewAsync_FeedInSubfolder_IsRead()
    {
        var report = await Service.PreviewAsync(Zip(_files, folder: "gtfs/"), Ct);

        Assert.True(report.CanImport, Describe(report));
        Assert.Equal(1, report.Counts.Trips);
    }

    [Fact]
    public async Task PreviewAsync_NotAZip_IsError()
    {
        var report = await Service.PreviewAsync(new MemoryStream("nije zip"u8.ToArray()), Ct);

        Assert.Equal("Datoteka nije ispravna ZIP arhiva.", Assert.Single(report.Errors).Message);
    }

    [Fact]
    public async Task ImportAsync_ExportedFeed_RoundTrips()
    {
        var source = new FakeGtfsExportRepository();
        var agency = new AgencyModel { Id = Guid.NewGuid(), Code = "GPP", Name = "GPP Osijek", Url = "https://www.gpp.hr", Timezone = "Europe/Zagreb", Lang = "hr", Email = "info@gpp.hr" };
        var station = new StopModel { Id = Guid.NewGuid(), Code = "AK", Name = "Kolodvor", Latitude = 45.55, Longitude = 18.69, LocationType = LocationType.Station, IsActive = true };
        var platform = new StopModel { Id = Guid.NewGuid(), Code = "AK-1", PublicCode = "101", Name = "Kolodvor, \"peron\" 1", Latitude = 45.5501, Longitude = 18.6901, ParentStationId = station.Id, PlatformCode = "1", ZoneId = "Z1", WheelchairBoarding = WheelchairBoarding.Accessible, IsActive = true };
        var end = new StopModel { Id = Guid.NewGuid(), Code = "C", Name = "Bolnica", Latitude = 45.57, Longitude = 18.72, ZoneId = "Z2", IsActive = true };
        var route = new RouteModel { Id = Guid.NewGuid(), AgencyId = agency.Id, Code = "L1", ShortName = "1", LongName = "Kolodvor - Bolnica", RouteType = (RouteType)700, Color = "1565C0", TextColor = "FFFFFF", SortOrder = 5, IsActive = true };
        var calendar = new ServiceCalendarModel { Id = Guid.NewGuid(), Code = "RD", Name = "Radni dan", Monday = true, Tuesday = true, Wednesday = true, Thursday = true, Friday = true, StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31) };
        var shape = new ShapeModel { Id = Guid.NewGuid(), Code = "S1", Points = [new ShapePointModel { Sequence = 1, Latitude = 45.5501, Longitude = 18.6901, DistTraveled = 0 }, new ShapePointModel { Sequence = 2, Latitude = 45.57, Longitude = 18.72, DistTraveled = 2500 }] };
        var trip = new TripModel { Id = Guid.NewGuid(), RouteId = route.Id, ServiceCalendarId = calendar.Id, Code = "T1", Headsign = "Bolnica", DirectionId = TripDirection.Inbound, BlockId = "B1", ShapeId = shape.Id, WheelchairAccessible = WheelchairAccessible.Accessible };

        source.Agencies.Add(agency);
        source.Stops.AddRange([station, platform, end]);
        source.Routes.Add(route);
        source.Calendars.Add(calendar);
        source.CalendarDates.Add(new CalendarDateModel { ServiceCalendarId = calendar.Id, Date = new DateOnly(2026, 12, 25), ExceptionType = CalendarExceptionType.Removed });
        source.Shapes.Add(shape);
        source.Trips.Add(trip);
        source.StopTimes.AddRange([
            new StopTimeModel { TripId = trip.Id, StopSequence = 1, StopId = platform.Id, ArrivalTime = Time("23:50:00"), DepartureTime = Time("23:55:00"), ShapeDistTraveled = 0 },
            new StopTimeModel { TripId = trip.Id, StopSequence = 2, StopId = end.Id, ArrivalTime = Time("24:10:00"), DepartureTime = Time("24:10:00"), DropOffType = PickupDropOffType.PhoneAgency, ShapeDistTraveled = 2500, Timepoint = false }
        ]);
        source.Fares.Add(new FareModel { Id = Guid.NewGuid(), Code = "F1", Price = 1.6m, CurrencyType = "EUR", PaymentMethod = FarePaymentMethod.BeforeBoarding, AgencyId = agency.Id, Rules = [new FareRuleModel { RouteId = route.Id, OriginId = "Z1", DestinationId = "Z2" }] });
        source.FeedInfo = new FeedInfoModel { PublisherName = "GPP", PublisherUrl = "https://www.gpp.hr", Lang = "hr", ContactEmail = "gtfs@gpp.hr" };

        var exported = await new GtfsExportService(source, new FixedTimeProvider(Now)).ExportAsync(Ct);
        var report = await Service.ImportAsync(new MemoryStream(exported.Content), null, Ct);

        Assert.True(report.Imported, Describe(report));
        Assert.Empty(report.Warnings);

        var feed = _repository.Feed!;
        Assert.Equivalent(new { agency.Code, agency.Name, agency.Url, agency.Timezone, agency.Lang, agency.Email }, Assert.Single(feed.Agencies));

        var importedPlatform = feed.Stops.Single(s => s.Code == "AK-1");
        Assert.Equivalent(new { platform.PublicCode, platform.Name, platform.Latitude, platform.Longitude, platform.PlatformCode, platform.ZoneId, platform.WheelchairBoarding }, importedPlatform);
        Assert.Equal(feed.Stops.Single(s => s.Code == "AK").Id, importedPlatform.ParentStationId);

        Assert.Equivalent(new { route.Code, route.ShortName, route.LongName, route.RouteType, route.Color, route.TextColor, route.SortOrder }, Assert.Single(feed.Routes));
        Assert.Equivalent(new { calendar.Code, calendar.Monday, calendar.Saturday, calendar.StartDate, calendar.EndDate }, Assert.Single(feed.Calendars));
        Assert.Equal(CalendarExceptionType.Removed, Assert.Single(feed.CalendarDates).ExceptionType);
        Assert.Equal([0, 2500], Assert.Single(feed.Shapes).Points.Select(p => p.DistTraveled));

        var importedTrip = Assert.Single(feed.Trips);
        Assert.Equivalent(new { trip.Code, trip.Headsign, trip.DirectionId, trip.BlockId, trip.WheelchairAccessible }, importedTrip);
        Assert.Equal(feed.Shapes[0].Id, importedTrip.ShapeId);
        Assert.Equal(
            [(Time("23:50:00"), Time("23:55:00"), true, PickupDropOffType.Regular), (Time("24:10:00"), Time("24:10:00"), false, PickupDropOffType.PhoneAgency)],
            feed.StopTimes.Select(st => (st.ArrivalTime, st.DepartureTime, st.Timepoint, st.DropOffType)));

        var fare = Assert.Single(feed.Fares);
        Assert.Equivalent(new { Code = "F1", Price = 1.6m, CurrencyType = "EUR", PaymentMethod = FarePaymentMethod.BeforeBoarding, Transfers = (FareTransfers?)null }, fare);
        Assert.Equal(feed.Routes[0].Id, Assert.Single(fare.Rules).RouteId);
        Assert.Equal("gtfs@gpp.hr", feed.FeedInfo?.ContactEmail);
    }

    private static int Time(string value)
    {
        Assert.True(GtfsTime.TryParse(value, out var seconds));
        return seconds;
    }

    private static string Describe(GtfsImportReport report) =>
        string.Join("; ", report.Errors.Concat(report.Warnings).Select(i => $"{i.File}: {i.Message} [{string.Join(", ", i.Examples)}]"));

    private static MemoryStream Zip(Dictionary<string, string> files, string folder = "")
    {
        var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in files)
            {
                using var writer = new StreamWriter(archive.CreateEntry(folder + name).Open(), new UTF8Encoding(true));
                writer.Write(content.Replace("\n", "\r\n"));
            }
        }

        buffer.Position = 0;
        return buffer;
    }
}
