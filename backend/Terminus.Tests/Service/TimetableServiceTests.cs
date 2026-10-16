using Terminus.Common.Exceptions;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Service;
using Terminus.Service.Common;
using Terminus.Tests.Fakes;

namespace Terminus.Tests.Service;

public class TimetableServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 12, 23, 8, 0, 0, TimeSpan.FromHours(1));

    private readonly FakeTimetableRepository _timetable = new();
    private readonly FakeRouteRepository _routes = new();
    private readonly FakeDepartureRepository _departures = new();
    private readonly List<IStopModel> _stops = [];

    private readonly RouteModel _route = new() { Id = Guid.NewGuid(), Code = "12", ShortName = "12" };
    private readonly ServiceCalendarModel _workdays;
    private readonly ServiceCalendarModel _saturday;
    private readonly StopModel _a, _b, _c, _d;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public TimetableServiceTests()
    {
        _routes.Routes.Add(_route);
        _timetable.Routes.Add(_route);

        _workdays = Calendar("RD", monday: true, tuesday: true, wednesday: true, thursday: true, friday: true);
        _saturday = Calendar("SUB", saturday: true);
        _departures.Calendars.AddRange([_workdays, _saturday]);

        _a = AddStop("A");
        _b = AddStop("B");
        _c = AddStop("C");
        _d = AddStop("D");
    }

    private TimetableService Service => new(_timetable, _routes, new FakeStopRepository(_stops), _departures);

    [Fact]
    public async Task GetRoutePatternsAsync_GroupsTripsByStopSequence_MostCommonFirst()
    {
        AddTrip(_workdays, TripDirection.Outbound, "Đakovo", (_a, "06:00"), (_b, "06:10"), (_c, "06:20"));
        AddTrip(_workdays, TripDirection.Outbound, "Đakovo", (_a, "07:00"), (_b, "07:10"), (_c, "07:20"));
        AddTrip(_workdays, TripDirection.Outbound, "B", (_a, "08:00"), (_b, "08:10"));
        AddTrip(_workdays, TripDirection.Inbound, "Osijek", (_c, "09:00"), (_a, "09:20"));

        var patterns = await Service.GetRoutePatternsAsync(_route.Id, Ct);

        Assert.Equal(3, patterns.Count);
        Assert.Equal(TripDirection.Outbound, patterns[0].DirectionId);
        Assert.Equal(2, patterns[0].TripCount);
        Assert.Equal("Đakovo", patterns[0].Headsign);
        Assert.Equal(["A", "B", "C"], patterns[0].Stops.Select(s => s.Code));
        Assert.Equal(["A", "B"], patterns[1].Stops.Select(s => s.Code));
        Assert.Equal(TripDirection.Inbound, patterns[2].DirectionId);
    }

    [Fact]
    public async Task GetRoutePatternsAsync_UnknownRoute_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Service.GetRoutePatternsAsync(Guid.NewGuid(), Ct));
    }

    [Fact]
    public async Task GetRouteTimetableAsync_ForDate_UsesCalendarsRunningThatDay()
    {
        AddTrip(_workdays, TripDirection.Outbound, null, (_a, "07:00"), (_b, "07:10"));
        AddTrip(_saturday, TripDirection.Outbound, null, (_a, "09:00"), (_b, "09:10"));

        // Subota 26.12.2026.
        var timetable = await Service.GetRouteTimetableAsync(
            _route.Id, new RouteTimetableQuery(new DateOnly(2026, 12, 26), null, null), Now, Ct);

        var trip = Assert.Single(timetable.Trips);
        Assert.Equal(_saturday.Id, trip.Trip.ServiceCalendarId);
        Assert.Equal([_saturday.Id], timetable.Calendars.Select(c => c.Id));
        Assert.Equal(new DateOnly(2026, 12, 26), timetable.Date);
    }

    [Fact]
    public async Task GetRouteTimetableAsync_WithoutDateOrCalendar_UsesToday()
    {
        AddTrip(_workdays, TripDirection.Outbound, null, (_a, "07:00"), (_b, "07:10"));
        AddTrip(_saturday, TripDirection.Outbound, null, (_a, "09:00"), (_b, "09:10"));

        // Now je srijeda 23.12.2026.
        var timetable = await Service.GetRouteTimetableAsync(_route.Id, new RouteTimetableQuery(null, null, null), Now, Ct);

        Assert.Equal(new DateOnly(2026, 12, 23), timetable.Date);
        Assert.Equal(_workdays.Id, Assert.Single(timetable.Trips).Trip.ServiceCalendarId);
    }

    [Fact]
    public async Task GetRouteTimetableAsync_BuildsMatrixWithGapsForSkippedStops()
    {
        var late = AddTrip(_workdays, TripDirection.Outbound, null, (_a, "08:00"), (_c, "08:15"), (_d, "08:30"));
        var early = AddTrip(_workdays, TripDirection.Outbound, null, (_a, "07:00"), (_b, "07:10"), (_c, "07:20"));

        var timetable = await Service.GetRouteTimetableAsync(
            _route.Id, new RouteTimetableQuery(null, _workdays.Id, TripDirection.Outbound), Now, Ct);

        Assert.Equal(["A", "B", "C", "D"], timetable.Stops.Select(s => s.Code));
        Assert.Equal([early.Id, late.Id], timetable.Trips.Select(t => t.Trip.Id));

        Assert.Equal([7 * 3600, 7 * 3600 + 600, 7 * 3600 + 1200, (int?)null],
            timetable.Trips[0].Times.Select(t => t?.DepartureTime));
        Assert.Equal([8 * 3600, (int?)null, 8 * 3600 + 900, 8 * 3600 + 1800],
            timetable.Trips[1].Times.Select(t => t?.DepartureTime));
    }

    [Fact]
    public async Task GetRouteTimetableAsync_FiltersDirection()
    {
        AddTrip(_workdays, TripDirection.Outbound, null, (_a, "07:00"), (_b, "07:10"));
        AddTrip(_workdays, TripDirection.Inbound, null, (_b, "08:00"), (_a, "08:10"));

        var timetable = await Service.GetRouteTimetableAsync(
            _route.Id, new RouteTimetableQuery(null, _workdays.Id, TripDirection.Inbound), Now, Ct);

        Assert.Equal(["B", "A"], timetable.Stops.Select(s => s.Code));
        Assert.Single(timetable.Trips);
    }

    [Fact]
    public async Task GetRouteTimetableAsync_DateAndCalendar_IsRejected()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() => Service.GetRouteTimetableAsync(
            _route.Id, new RouteTimetableQuery(new DateOnly(2026, 12, 23), _workdays.Id, null), Now, Ct));

        Assert.Contains("Date", ex.Errors.Keys);
    }

    [Fact]
    public async Task GetRouteTimetableAsync_UnknownCalendar_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Service.GetRouteTimetableAsync(
            _route.Id, new RouteTimetableQuery(null, Guid.NewGuid(), null), Now, Ct));
    }

    [Fact]
    public async Task GetStopRoutesAsync_Station_IncludesRoutesOfPlatforms()
    {
        var station = AddStop("AK", LocationType.Station);
        var platform = AddStop("AK-1");
        platform.ParentStationId = station.Id;
        _departures.Stops.Add(platform);

        AddTrip(_workdays, TripDirection.Outbound, null, (platform, "07:00"), (_b, "07:10"));

        var routes = await Service.GetStopRoutesAsync(station.Id, Ct);

        Assert.Equal([_route.Id], routes.Select(r => r.Id));
    }

    [Fact]
    public async Task GetStopRoutesAsync_StopWithoutTrips_ReturnsEmpty()
    {
        AddTrip(_workdays, TripDirection.Outbound, null, (_a, "07:00"), (_b, "07:10"));

        Assert.Empty(await Service.GetStopRoutesAsync(_d.Id, Ct));
    }

    private StopModel AddStop(string code, LocationType locationType = LocationType.StopOrPlatform)
    {
        var stop = new StopModel { Id = Guid.NewGuid(), Code = code, Name = code, LocationType = locationType };
        _stops.Add(stop);
        return stop;
    }

    private TripModel AddTrip(
        ServiceCalendarModel calendar,
        TripDirection direction,
        string? headsign,
        params (StopModel Stop, string Time)[] stops)
    {
        var trip = new TripModel
        {
            Id = Guid.NewGuid(),
            RouteId = _route.Id,
            ServiceCalendarId = calendar.Id,
            Code = $"T{_timetable.Trips.Count + 1}",
            DirectionId = direction,
            Headsign = headsign
        };

        var stopTimes = stops
            .Select((s, i) =>
            {
                var seconds = (int)TimeSpan.Parse(s.Time).TotalSeconds;
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
        bool friday = false,
        bool saturday = false) => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        Name = code,
        Monday = monday,
        Tuesday = tuesday,
        Wednesday = wednesday,
        Thursday = thursday,
        Friday = friday,
        Saturday = saturday,
        StartDate = new DateOnly(2026, 1, 1),
        EndDate = new DateOnly(2026, 12, 31)
    };
}
