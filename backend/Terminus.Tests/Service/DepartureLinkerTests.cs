using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Service.Gtfs.Import;

namespace Terminus.Tests.Service;

public class DepartureLinkerTests
{
    private static readonly Guid Workday = Guid.NewGuid();
    private static readonly Guid Saturday = Guid.NewGuid();
    private static readonly Guid Route = Guid.NewGuid();
    private static readonly Guid Variant = Guid.NewGuid();
    private static readonly Guid StopA = Guid.NewGuid();
    private static readonly Guid StopB = Guid.NewGuid();

    private readonly List<ITripModel> _trips = [];
    private readonly List<IStopTimeModel> _stopTimes = [];

    private TripModel Trip(Guid calendar, int departure = 21600, string? headsign = "Sopot", string? block = null)
    {
        var trip = new TripModel { Id = Guid.NewGuid(), RouteId = Route, VariantId = Variant, ServiceCalendarId = calendar, Headsign = headsign, BlockId = block };
        _trips.Add(trip);
        _stopTimes.Add(new StopTimeModel { TripId = trip.Id, StopSequence = 1, StopId = StopA, ArrivalTime = departure, DepartureTime = departure });
        _stopTimes.Add(new StopTimeModel { TripId = trip.Id, StopSequence = 2, StopId = StopB, ArrivalTime = departure + 600, DepartureTime = departure + 600 });
        return trip;
    }

    private int Link() => DepartureLinker.Link(new GtfsFeedData
    {
        Agencies = [],
        Stops = [],
        Routes = [],
        Calendars = [],
        CalendarDates = [],
        Shapes = [],
        Trips = _trips,
        StopTimes = _stopTimes,
        Fares = []
    });

    [Fact]
    public void SameTrip_InTwoCalendars_IsOneDeparture()
    {
        var workday = Trip(Workday);
        var saturday = Trip(Saturday);

        Assert.Equal(1, Link());

        Assert.NotNull(workday.DepartureId);
        Assert.Equal(workday.DepartureId, saturday.DepartureId);
    }

    [Fact]
    public void DifferentVehicle_IsStillTheSameDeparture()
    {
        var workday = Trip(Workday, block: "B1");
        var saturday = Trip(Saturday, block: "B2");

        Link();

        Assert.Equal(workday.DepartureId, saturday.DepartureId);
        Assert.NotNull(workday.DepartureId);
    }

    [Fact]
    public void DifferentTimes_AreNotLinked()
    {
        var workday = Trip(Workday, departure: 21600);
        var saturday = Trip(Saturday, departure: 21660);

        Assert.Equal(0, Link());

        Assert.Null(workday.DepartureId);
        Assert.Null(saturday.DepartureId);
    }

    [Fact]
    public void DifferentHeadsign_IsNotLinked()
    {
        // Grafikon bi pri izmjeni jedne vožnje prepisao natpis druge.
        var workday = Trip(Workday, headsign: "Sopot");
        var saturday = Trip(Saturday, headsign: "Dubec");

        Link();

        Assert.Null(workday.DepartureId);
        Assert.Null(saturday.DepartureId);
    }

    [Fact]
    public void TripOnlyInOneCalendar_HasNoDeparture()
    {
        var trip = Trip(Workday);

        Assert.Equal(0, Link());

        Assert.Null(trip.DepartureId);
    }

    [Fact]
    public void TwinTrips_InACalendar_AreSeparateDepartures()
    {
        // Polazak ima jednu vožnju po kalendaru: grafikon bi inače obrisao blizanca.
        var first = Trip(Workday);
        var twin = Trip(Workday);
        var saturday = Trip(Saturday);

        Assert.Equal(1, Link());

        Assert.Equal(first.DepartureId, saturday.DepartureId);
        Assert.Null(twin.DepartureId);
    }
}
