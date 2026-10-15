using System.IO.Compression;
using System.Text;
using Terminus.Common.Exceptions;
using Terminus.Model;
using Terminus.Service.Gtfs;
using Terminus.Tests.Fakes;

namespace Terminus.Tests.Service;

public class GtfsExportServiceTests
{
    private readonly FakeGtfsExportRepository _repository = new();
    private readonly GtfsExportService _service;

    public GtfsExportServiceTests()
    {
        _service = new GtfsExportService(_repository);
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

        var feed = await _service.ExportAsync(TestContext.Current.CancellationToken);
        var agencyRows = ReadCsv(feed.Content, "agency.txt");

        Assert.Equal("agency_id", agencyRows[0][0]);
        Assert.Equivalent(
            new[] { operatorA.Id.ToString(), operatorB.Id.ToString() },
            agencyRows.Skip(1).Select(row => row[0]).ToArray(),
            strict: true);
    }

    [Fact]
    public async Task ExportAsync_RoutesReferenceTheirAgency()
    {
        var operatorA = Agency("Prijevoznik A");
        var operatorB = Agency("Prijevoznik B");
        _repository.Agencies.AddRange([operatorA, operatorB]);
        AddRouteWithTrip(operatorA, "L1");
        AddRouteWithTrip(operatorB, "L2");

        var feed = await _service.ExportAsync(TestContext.Current.CancellationToken);
        var routeRows = ReadCsv(feed.Content, "routes.txt");

        Assert.Equal(["route_id", "agency_id"], routeRows[0].Take(2));
        var agencyByRoute = routeRows.Skip(1).ToDictionary(row => row[0], row => row[1]);
        Assert.Equal(operatorA.Id.ToString(), agencyByRoute["L1"]);
        Assert.Equal(operatorB.Id.ToString(), agencyByRoute["L2"]);
    }

    [Fact]
    public async Task ExportAsync_InactiveRoute_IsLeftOut()
    {
        var agency = Agency("Prijevoznik");
        _repository.Agencies.Add(agency);
        AddRouteWithTrip(agency, "L1");
        AddRouteWithTrip(agency, "L2").IsActive = false;

        var feed = await _service.ExportAsync(TestContext.Current.CancellationToken);
        var routeIds = ReadCsv(feed.Content, "routes.txt").Skip(1).Select(row => row[0]);

        Assert.Equal(["L1"], routeIds);
    }

    [Fact]
    public async Task ExportAsync_FileNameAndPublisherUseFirstAgency()
    {
        var first = Agency("Gradski prijevoz Đakovo d.o.o.");
        var second = Agency("Drugi prijevoznik");
        _repository.Agencies.AddRange([first, second]);
        AddRouteWithTrip(second, "L1");

        var feed = await _service.ExportAsync(TestContext.Current.CancellationToken);
        var feedInfo = ReadCsv(feed.Content, "feed_info.txt");

        Assert.StartsWith("gtfs-gradski-prijevoz-dakovo-d-o-o-", feed.FileName);
        Assert.EndsWith(".zip", feed.FileName);
        Assert.Equal(first.Name, feedInfo[1][0]);
    }

    private static AgencyModel Agency(string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Url = "https://example.com",
        Timezone = "Europe/Zagreb",
        Lang = "hr"
    };

    /// <summary>Linija s jednom vožnjom radnim danom između dvije nove stanice.</summary>
    private RouteModel AddRouteWithTrip(AgencyModel agency, string code)
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

        return route;
    }

    /// <summary>Retci CSV datoteke iz ZIP-a; vrijednosti u testovima nemaju zareze ni navodnike.</summary>
    private static List<string[]> ReadCsv(byte[] zip, string fileName)
    {
        using var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read);
        var entry = archive.GetEntry(fileName) ?? throw new InvalidOperationException($"Feed nema {fileName}.");

        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        return reader.ReadToEnd()
            .Split("\r\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split(','))
            .ToList();
    }
}
