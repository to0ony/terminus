using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Service;
using Terminus.Tests.Fakes;

namespace Terminus.Tests.Service;

public class LiveMapServiceTests
{
    // Srijeda 23. 12. 2026. u 8 h u Zagrebu.
    private static readonly DateTimeOffset Now = new(2026, 12, 23, 8, 0, 0, TimeSpan.FromHours(1));
    private static readonly DateOnly Wednesday = new(2026, 12, 23);

    private readonly FakeTimetableRepository _timetable = new();
    private readonly FakeRouteRepository _routes = new();
    private readonly FakeRouteVariantRepository _variants = new();
    private readonly FakeShapeRepository _shapes = new();
    private readonly FakeDepartureRepository _departures = new();
    private readonly List<IStopModel> _stops = [];

    private readonly RouteModel _route = new() { Id = Guid.NewGuid(), Code = "12", ShortName = "12" };
    private readonly ServiceCalendarModel _workdays = Calendar("RD", monday: true, tuesday: true, wednesday: true, thursday: true, friday: true);
    private readonly ServiceCalendarModel _tuesday = Calendar("UT", tuesday: true);
    private readonly StopModel _a, _b, _c;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public LiveMapServiceTests()
    {
        _routes.Routes.Add(_route);
        _timetable.Routes.Add(_route);
        _departures.Calendars.AddRange([_workdays, _tuesday]);

        _a = AddStop("A");
        _b = AddStop("B");
        _c = AddStop("C");
    }

    private LiveMapService Service => new(_timetable, _routes, _variants, new FakeStopRepository(_stops), _shapes, _departures);

    [Fact]
    public async Task GetRouteDayAsync_WithoutDate_UsesTodayInAgencyTimeZone()
    {
        AddTrip(_workdays, null, (_a, "06:00"), (_b, "06:10"), (_c, "06:20"));
        AddTrip(_tuesday, null, (_a, "07:00"), (_c, "07:20"));

        var day = await Service.GetRouteDayAsync(_route.Id, null, Now, Ct);

        Assert.Equal(Wednesday, day.Date);
        Assert.Equal("Europe/Zagreb", day.TimeZone);
        var trip = Assert.Single(day.Trips);
        Assert.Equal([6 * 3600, 6 * 3600 + 600, 6 * 3600 + 1200], trip.StopTimes.Select(st => st.DepartureTime));
        Assert.Equal("C", trip.Headsign);
        Assert.False(trip.PreviousDay);
        Assert.Equal(3, day.Stops.Count);
    }

    [Fact]
    public async Task GetRouteDayAsync_IncludesTripsOfThePreviousDayAfterMidnight()
    {
        // Utorak: jedna vožnja završi prije ponoći, druga iza nje.
        AddTrip(_tuesday, "Kasna", (_a, "22:00"), (_c, "22:30"));
        AddTrip(_tuesday, "Noćna", (_a, "23:50"), (_b, "24:05"), (_c, "24:20"));

        var day = await Service.GetRouteDayAsync(_route.Id, Wednesday, Now, Ct);

        var trip = Assert.Single(day.Trips);
        Assert.Equal("Noćna", trip.Headsign);
        Assert.True(trip.PreviousDay);
        Assert.Equal([-600, 300, 1200], trip.StopTimes.Select(st => st.ArrivalTime));
    }

    [Fact]
    public async Task GetRouteDayAsync_UsesTheVariantShapeAndSkipsStopsWithoutCoordinates()
    {
        var shape = new ShapeModel
        {
            Id = Guid.NewGuid(),
            Code = "S1",
            Points = [new ShapePointModel { Sequence = 1, Latitude = 45.5, Longitude = 18.6 }, new ShapePointModel { Sequence = 2, Latitude = 45.6, Longitude = 18.7 }]
        };
        _shapes.Shapes.Add(shape);
        var variant = new RouteVariantModel { Id = Guid.NewGuid(), RouteId = _route.Id, Name = "Glavna", ShapeId = shape.Id };
        _variants.Variants.Add(variant);
        var nowhere = AddStop("X", coordinates: false);

        var trip = AddTrip(_workdays, null, (_a, "06:00"), (nowhere, "06:05"), (_c, "06:20"));
        trip.VariantId = variant.Id;

        var day = await Service.GetRouteDayAsync(_route.Id, Wednesday, Now, Ct);

        var live = Assert.Single(day.Trips);
        Assert.Equal(shape.Id, live.ShapeId);
        Assert.Equal([_a.Id, _c.Id], live.StopTimes.Select(st => st.StopId));
        Assert.Equal(shape.Id, Assert.Single(day.Shapes).Id);
    }

    [Fact]
    public async Task GetNetworkDayAsync_ReturnsTripsOfAllRoutes()
    {
        var other = new RouteModel { Id = Guid.NewGuid(), Code = "5", ShortName = "5", Color = "1E88E5" };
        _routes.Routes.Add(other);
        AddTrip(_workdays, null, (_a, "06:00"), (_c, "06:20"));
        var trip = AddTrip(_workdays, null, (_c, "07:00"), (_b, "07:15"));
        trip.RouteId = other.Id;

        var day = await Service.GetNetworkDayAsync(Wednesday, Now, Ct);

        Assert.Equal([_route.Id, other.Id], day.Trips.Select(t => t.RouteId));
        Assert.Equal(["12", "5"], day.Routes.Select(r => r.Code).Order());
    }

    private StopModel AddStop(string code, bool coordinates = true)
    {
        var stop = new StopModel
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = code,
            Latitude = coordinates ? 45.5 + _stops.Count / 100.0 : null,
            Longitude = coordinates ? 18.6 : null
        };
        _stops.Add(stop);
        return stop;
    }

    private TripModel AddTrip(ServiceCalendarModel calendar, string? headsign, params (StopModel Stop, string Time)[] stops)
    {
        var trip = new TripModel
        {
            Id = Guid.NewGuid(),
            RouteId = _route.Id,
            ServiceCalendarId = calendar.Id,
            Code = $"T{_timetable.Trips.Count + 1}",
            DirectionId = TripDirection.Outbound,
            Headsign = headsign
        };

        var stopTimes = stops
            .Select((s, i) =>
            {
                var parts = s.Time.Split(':');
                var seconds = int.Parse(parts[0]) * 3600 + int.Parse(parts[1]) * 60;
                return (IStopTimeModel)new StopTimeModel
                {
                    TripId = trip.Id,
                    StopSequence = i + 1,
                    StopId = s.Stop.Id,
                    ArrivalTime = seconds,
                    DepartureTime = seconds
                };
            })
            .ToList();

        _timetable.Trips.Add(new TripStopTimes(trip, stopTimes));
        return trip;
    }

    private static ServiceCalendarModel Calendar(
        string code,
        bool monday = false,
        bool tuesday = false,
        bool wednesday = false,
        bool thursday = false,
        bool friday = false) => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        Name = code,
        Monday = monday,
        Tuesday = tuesday,
        Wednesday = wednesday,
        Thursday = thursday,
        Friday = friday,
        StartDate = new DateOnly(2026, 1, 1),
        EndDate = new DateOnly(2026, 12, 31)
    };
}
