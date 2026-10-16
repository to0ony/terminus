using System.IO.Compression;
using System.Text;
using Terminus.Common.Exceptions;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Service.Gtfs;
using Terminus.Tests.Fakes;

namespace Terminus.Tests.Service;

public class GtfsExportServiceTests
{
    private readonly FakeGtfsExportRepository _repository = new();
    private readonly GtfsExportService _service;

    public GtfsExportServiceTests()
    {
        _service = new GtfsExportService(_repository, new FixedTimeProvider(new DateTimeOffset(2026, 10, 16, 10, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public async Task ExportAsync_NoAgency_ThrowsUnprocessable()
    {
        var ex = await Assert.ThrowsAsync<UnprocessableException>(
            () => _service.ExportAsync(TestContext.Current.CancellationToken));

        Assert.Contains("Nije unesen nijedan prijevoznik.", ex.Problems);
    }

    [Fact]
    public async Task ExportAsync_NoTrips_ThrowsUnprocessable()
    {
        _repository.Agencies.Add(Agency("Prijevoznik"));

        var ex = await Assert.ThrowsAsync<UnprocessableException>(
            () => _service.ExportAsync(TestContext.Current.CancellationToken));

        Assert.Contains("Feed nema nijednu vožnju na aktivnoj liniji.", ex.Problems);
    }

    [Fact]
    public async Task ExportAsync_WritesOnlyAgenciesOfExportedRoutes()
    {
        var unused = Agency("Bez linija");
        var operatorA = Agency("Prijevoznik A");
        var operatorB = Agency("Prijevoznik B");
        _repository.Agencies.AddRange([unused, operatorA, operatorB]);
        AddRouteWithTrip(operatorA, "L1");
        AddRouteWithTrip(operatorB, "L2");

        var feed = await ExportAsync();

        Assert.Equivalent(new[] { operatorA.Code, operatorB.Code }, feed.Column("agency.txt", "agency_id"), strict: true);
    }

    [Fact]
    public async Task ExportAsync_RoutesReferenceAgencyCode()
    {
        var operatorA = Agency("Prijevoznik A");
        var operatorB = Agency("Prijevoznik B");
        _repository.Agencies.AddRange([operatorA, operatorB]);
        AddRouteWithTrip(operatorA, "L1");
        AddRouteWithTrip(operatorB, "L2");

        var feed = await ExportAsync();
        var agencyByRoute = feed.Rows("routes.txt").ToDictionary(row => row["route_id"], row => row["agency_id"]);

        Assert.Equal(operatorA.Code, agencyByRoute["L1"]);
        Assert.Equal(operatorB.Code, agencyByRoute["L2"]);
    }

    [Fact]
    public async Task ExportAsync_InactiveRoute_IsLeftOut()
    {
        var agency = Agency("Prijevoznik");
        _repository.Agencies.Add(agency);
        AddRouteWithTrip(agency, "L1");
        AddRouteWithTrip(agency, "L2").Route.IsActive = false;

        var feed = await ExportAsync();

        Assert.Equal(["L1"], feed.Column("routes.txt", "route_id"));
    }

    [Fact]
    public async Task ExportAsync_ExtendedRouteType_IsWrittenAsNumber()
    {
        var agency = Agency("Prijevoznik");
        _repository.Agencies.Add(agency);
        AddRouteWithTrip(agency, "L1").Route.RouteType = (RouteType)700;

        var feed = await ExportAsync();

        Assert.Equal(["700"], feed.Column("routes.txt", "route_type"));
    }

    [Fact]
    public async Task ExportAsync_WithoutFeedInfo_FileNameAndPublisherUseFirstAgency()
    {
        var first = Agency("Gradski prijevoz Đakovo d.o.o.");
        var second = Agency("Drugi prijevoznik");
        _repository.Agencies.AddRange([first, second]);
        AddRouteWithTrip(second, "L1");

        var feed = await ExportAsync();

        Assert.Equal("gtfs-gradski-prijevoz-dakovo-d-o-o-20261016.zip", feed.FileName);
        Assert.Equal([first.Name], feed.Column("feed_info.txt", "feed_publisher_name"));
    }

    [Fact]
    public async Task ExportAsync_WithFeedInfo_UsesItsPublisherAndContact()
    {
        var agency = Agency("Prijevoznik");
        _repository.Agencies.Add(agency);
        AddRouteWithTrip(agency, "L1");
        _repository.FeedInfo = new FeedInfoModel
        {
            PublisherName = "Županija",
            PublisherUrl = "https://zupanija.example",
            Lang = "hr",
            DefaultLang = "en",
            ContactEmail = "gtfs@zupanija.example"
        };

        var feed = await ExportAsync();
        var info = Assert.Single(feed.Rows("feed_info.txt"));

        Assert.StartsWith("gtfs-zupanija-", feed.FileName);
        Assert.Equal("Županija", info["feed_publisher_name"]);
        Assert.Equal("https://zupanija.example", info["feed_publisher_url"]);
        Assert.Equal("en", info["default_lang"]);
        Assert.Equal("gtfs@zupanija.example", info["feed_contact_email"]);
    }

    [Fact]
    public async Task ExportAsync_StopFields_AreWritten()
    {
        var agency = Agency("Prijevoznik");
        _repository.Agencies.Add(agency);
        var (_, _, from) = AddRouteWithTrip(agency, "L1");
        from.PublicCode = "1042";
        from.ZoneId = "Z1";
        from.TtsName = "Polazište glavni kolodvor";

        var feed = await ExportAsync();
        var row = feed.Rows("stops.txt").Single(r => r["stop_id"] == from.Code);

        Assert.Equal("1042", row["stop_code"]);
        Assert.Equal("Z1", row["zone_id"]);
        Assert.Equal("Polazište glavni kolodvor", row["tts_stop_name"]);
        Assert.Equal("45.55", row["stop_lat"]);
    }

    [Fact]
    public async Task ExportAsync_StationChildren_EntrancesAndBoardingAreasAreIncluded()
    {
        var agency = Agency("Prijevoznik");
        _repository.Agencies.Add(agency);
        var (_, _, platform) = AddRouteWithTrip(agency, "L1");

        var station = new StopModel { Id = Guid.NewGuid(), Code = "ST", Name = "Kolodvor", Latitude = 45.5, Longitude = 18.7, LocationType = LocationType.Station };
        var entrance = new StopModel { Id = Guid.NewGuid(), Code = "ST-E1", Name = "Ulaz", Latitude = 45.5, Longitude = 18.7, LocationType = LocationType.Entrance, ParentStationId = station.Id };
        var boarding = new StopModel { Id = Guid.NewGuid(), Code = "ST-B1", Name = "Ukrcaj", LocationType = LocationType.BoardingArea, ParentStationId = platform.Id };
        var otherEntrance = new StopModel { Id = Guid.NewGuid(), Code = "X-E1", Name = "Drugi ulaz", Latitude = 45.5, Longitude = 18.7, LocationType = LocationType.Entrance, ParentStationId = Guid.NewGuid() };
        platform.ParentStationId = station.Id;
        _repository.Stops.AddRange([station, entrance, boarding, otherEntrance]);

        var feed = await ExportAsync();
        var stops = feed.Rows("stops.txt").ToDictionary(r => r["stop_id"]);

        Assert.Contains("ST", stops.Keys);
        Assert.Equal("ST", stops["ST-E1"]["parent_station"]);
        Assert.Equal(platform.Code, stops["ST-B1"]["parent_station"]);
        Assert.Equal("", stops["ST-B1"]["stop_lat"]);
        Assert.DoesNotContain("X-E1", stops.Keys);
    }

    [Fact]
    public async Task ExportAsync_OnlyShapesUsedByExportedTripsAreWritten()
    {
        var agency = Agency("Prijevoznik");
        _repository.Agencies.Add(agency);
        var used = Shape("SH-1");
        var unused = Shape("SH-2");
        _repository.Shapes.AddRange([used, unused]);
        var (_, trip, _) = AddRouteWithTrip(agency, "L1");
        trip.ShapeId = used.Id;

        var feed = await ExportAsync();

        Assert.Equal(["SH-1"], feed.Column("trips.txt", "shape_id"));
        Assert.Equal(["SH-1", "SH-1"], feed.Column("shapes.txt", "shape_id"));
        Assert.Equal(["1", "2"], feed.Column("shapes.txt", "shape_pt_sequence"));
    }

    [Fact]
    public async Task ExportAsync_WithoutShapesOrFares_LeavesOptionalFilesOut()
    {
        var agency = Agency("Prijevoznik");
        _repository.Agencies.Add(agency);
        AddRouteWithTrip(agency, "L1");

        var feed = await ExportAsync();

        Assert.False(feed.Has("shapes.txt"));
        Assert.False(feed.Has("fare_attributes.txt"));
        Assert.False(feed.Has("calendar_dates.txt"));
    }

    [Fact]
    public async Task ExportAsync_Fares_WritesAttributesAndRules()
    {
        var agency = Agency("Prijevoznik");
        _repository.Agencies.Add(agency);
        var (route, _, from) = AddRouteWithTrip(agency, "L1");
        from.ZoneId = "Z1";
        _repository.Fares.Add(new FareModel
        {
            Id = Guid.NewGuid(),
            Code = "F1",
            Price = 1.5m,
            CurrencyType = "EUR",
            PaymentMethod = FarePaymentMethod.OnBoard,
            AgencyId = agency.Id,
            Rules = [new FareRuleModel { RouteId = route.Id, OriginId = "Z1" }]
        });

        var feed = await ExportAsync();
        var fare = Assert.Single(feed.Rows("fare_attributes.txt"));
        var rule = Assert.Single(feed.Rows("fare_rules.txt"));

        Assert.Equal("1.5", fare["price"]);
        Assert.Equal("", fare["transfers"]);
        Assert.Equal(agency.Code, fare["agency_id"]);
        Assert.Equal("L1", rule["route_id"]);
        Assert.Equal("Z1", rule["origin_id"]);
    }

    [Fact]
    public async Task ExportAsync_FareOnlyForInactiveRoute_IsLeftOut()
    {
        var agency = Agency("Prijevoznik");
        _repository.Agencies.Add(agency);
        AddRouteWithTrip(agency, "L1");
        var (inactive, _, _) = AddRouteWithTrip(agency, "L2");
        inactive.IsActive = false;
        _repository.Fares.Add(new FareModel
        {
            Id = Guid.NewGuid(),
            Code = "F-L2",
            Price = 2m,
            CurrencyType = "EUR",
            Rules = [new FareRuleModel { RouteId = inactive.Id }]
        });

        var feed = await ExportAsync();

        Assert.False(feed.Has("fare_attributes.txt"));
    }

    [Fact]
    public async Task ExportAsync_FareWithUnknownZone_ThrowsUnprocessable()
    {
        var agency = Agency("Prijevoznik");
        _repository.Agencies.Add(agency);
        AddRouteWithTrip(agency, "L1");
        _repository.Fares.Add(new FareModel
        {
            Id = Guid.NewGuid(),
            Code = "F1",
            Price = 1m,
            CurrencyType = "EUR",
            Rules = [new FareRuleModel { OriginId = "NEPOSTOJECA" }]
        });

        var ex = await Assert.ThrowsAsync<UnprocessableException>(
            () => _service.ExportAsync(TestContext.Current.CancellationToken));

        Assert.Contains(ex.Problems, p => p.Contains("NEPOSTOJECA"));
    }

    [Fact]
    public async Task ExportAsync_FareWithoutAgencyInMultiAgencyFeed_ThrowsUnprocessable()
    {
        var operatorA = Agency("Prijevoznik A");
        var operatorB = Agency("Prijevoznik B");
        _repository.Agencies.AddRange([operatorA, operatorB]);
        AddRouteWithTrip(operatorA, "L1");
        AddRouteWithTrip(operatorB, "L2");
        _repository.Fares.Add(new FareModel { Id = Guid.NewGuid(), Code = "F1", Price = 1m, CurrencyType = "EUR" });

        var ex = await Assert.ThrowsAsync<UnprocessableException>(
            () => _service.ExportAsync(TestContext.Current.CancellationToken));

        Assert.Contains("Cijena 'F1' nema prijevoznika, a feed ih ima više.", ex.Problems);
    }

    private async Task<ExportedFeed> ExportAsync()
    {
        var file = await _service.ExportAsync(TestContext.Current.CancellationToken);
        return new ExportedFeed(file.FileName, file.Content);
    }

    private static AgencyModel Agency(string name) => new()
    {
        Id = Guid.NewGuid(),
        Code = name.Replace(' ', '-'),
        Name = name,
        Url = "https://example.com",
        Timezone = "Europe/Zagreb",
        Lang = "hr"
    };

    private static ShapeModel Shape(string code) => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        Points =
        [
            new ShapePointModel { Sequence = 1, Latitude = 45.55, Longitude = 18.69 },
            new ShapePointModel { Sequence = 2, Latitude = 45.31, Longitude = 18.41 }
        ]
    };

    /// <summary>Linija s jednom vožnjom radnim danom između dvije nove stanice.</summary>
    private (RouteModel Route, TripModel Trip, StopModel From) AddRouteWithTrip(AgencyModel agency, string code)
    {
        var route = new RouteModel { Id = Guid.NewGuid(), AgencyId = agency.Id, Code = code, ShortName = code };
        var calendar = new ServiceCalendarModel
        {
            Id = Guid.NewGuid(),
            Code = $"{code}-RD",
            Name = "Radni dan",
            Monday = true,
            Tuesday = true,
            Wednesday = true,
            Thursday = true,
            Friday = true,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31)
        };
        var from = new StopModel { Id = Guid.NewGuid(), Code = $"{code}-A", Name = "Polazište", Latitude = 45.55, Longitude = 18.69 };
        var to = new StopModel { Id = Guid.NewGuid(), Code = $"{code}-B", Name = "Odredište", Latitude = 45.31, Longitude = 18.41 };
        var trip = new TripModel { Id = Guid.NewGuid(), RouteId = route.Id, ServiceCalendarId = calendar.Id, Code = $"{code}-0615" };

        _repository.Routes.Add(route);
        _repository.Calendars.Add(calendar);
        _repository.Stops.AddRange([from, to]);
        _repository.Trips.Add(trip);
        _repository.StopTimes.AddRange(
        [
            new StopTimeModel { TripId = trip.Id, StopSequence = 1, StopId = from.Id, ArrivalTime = 22500, DepartureTime = 22500 },
            new StopTimeModel { TripId = trip.Id, StopSequence = 2, StopId = to.Id, ArrivalTime = 25500, DepartureTime = 25500 }
        ]);

        return (route, trip, from);
    }

    /// <summary>Čita CSV datoteke iz ZIP-a; vrijednosti u testovima nemaju zareze ni navodnike.</summary>
    private sealed class ExportedFeed(string fileName, byte[] zip)
    {
        public string FileName { get; } = fileName;

        public bool Has(string file)
        {
            using var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read);
            return archive.GetEntry(file) is not null;
        }

        /// <summary>Retci datoteke kao rječnici stupac → vrijednost.</summary>
        public List<Dictionary<string, string>> Rows(string file)
        {
            using var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read);
            var entry = archive.GetEntry(file) ?? throw new InvalidOperationException($"Feed nema {file}.");

            using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
            var lines = reader.ReadToEnd().Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            var header = lines[0].Split(',');

            return lines.Skip(1)
                .Select(line => line.Split(','))
                .Select(values => header.Select((name, i) => (name, value: values[i])).ToDictionary(x => x.name, x => x.value))
                .ToList();
        }

        public List<string> Column(string file, string column) => Rows(file).Select(r => r[column]).ToList();
    }
}
