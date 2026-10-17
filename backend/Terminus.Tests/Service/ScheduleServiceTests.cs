using Terminus.Common.Exceptions;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Service;
using Terminus.Service.Common;
using Terminus.Tests.Fakes;

namespace Terminus.Tests.Service;

public class ScheduleServiceTests
{
    private static readonly DateTime Loaded = new(2026, 10, 17, 10, 0, 0, DateTimeKind.Utc);

    private readonly RouteModel _route = new() { Id = Guid.NewGuid(), Code = "6", ShortName = "6" };
    private readonly ServiceCalendarModel _workdays = new() { Id = Guid.NewGuid(), Code = "RD", Name = "Radni dan" };
    private readonly ServiceCalendarModel _sunday = new() { Id = Guid.NewGuid(), Code = "NED", Name = "Nedjelja" };
    private readonly StopModel[] _stops = [Stop("A"), Stop("B"), Stop("C")];

    private readonly FakeTimetableRepository _timetable = new();
    private readonly FakeScheduleRepository _schedule = new();
    private readonly ScheduleService _service;

    public ScheduleServiceTests()
    {
        var routes = new FakeRouteRepository();
        routes.Routes.Add(_route);
        var departures = new FakeDepartureRepository();
        departures.Calendars.Add(_workdays);
        departures.Calendars.Add(_sunday);

        _service = new ScheduleService(routes, _timetable, _schedule, new FakeStopRepository(_stops), departures, new FakeShapeRepository());
    }

    [Fact]
    public async Task GetAsync_RowsCoverAllCalendarsButTripsOnlyTheChosenOne()
    {
        AddTrip(_workdays, [_stops[0], _stops[1]], 7 * 3600);
        AddTrip(_sunday, [_stops[0], _stops[1], _stops[2]], 9 * 3600);

        var schedule = await _service.GetAsync(_route.Id, TripDirection.Outbound, _workdays.Id, TestContext.Current.CancellationToken);

        Assert.Equal(["A", "B", "C"], schedule.Stops.Select(s => s.Code));
        var trip = Assert.Single(schedule.Trips);
        Assert.Equal(_workdays.Id, trip.Trip.ServiceCalendarId);
        Assert.Equal(1, schedule.TripCounts[_sunday.Id]);
    }

    [Fact]
    public async Task SaveAsync_NewTripGetsCodeRouteAndDirection()
    {
        var change = new ScheduleTripChange(NewTrip(_workdays), StopTimes(7 * 3600, 7 * 3600 + 300), null);

        await _service.SaveAsync(_route.Id, new ScheduleChanges(TripDirection.Outbound, [change], []), TestContext.Current.CancellationToken);

        var saved = Assert.Single(_schedule.Saved);
        Assert.NotEqual(Guid.Empty, saved.Trip.Id);
        Assert.StartsWith("6-", saved.Trip.Code);
        Assert.Equal(_route.Id, saved.Trip.RouteId);
        Assert.Equal(TripDirection.Outbound, saved.Trip.DirectionId);
        Assert.Equal([1, 2], saved.StopTimes.Select(st => st.StopSequence));
        Assert.All(saved.StopTimes, st => Assert.Equal(saved.Trip.Id, st.TripId));
    }

    [Fact]
    public async Task SaveAsync_TimesGoingBackwards_IsRejected()
    {
        var change = new ScheduleTripChange(NewTrip(_workdays), StopTimes(7 * 3600, 7 * 3600 - 60), null);

        var error = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.SaveAsync(_route.Id, new ScheduleChanges(TripDirection.Outbound, [change], []), TestContext.Current.CancellationToken));

        Assert.Contains("Upserts[0].StopTimes[1].ArrivalTime", error.Errors.Keys);
    }

    [Fact]
    public async Task SaveAsync_TripChangedByAnotherUser_IsConflict()
    {
        var existing = ExistingTrip();
        var change = new ScheduleTripChange(Copy(existing), StopTimes(8 * 3600, 8 * 3600 + 300), Loaded.AddSeconds(-5));

        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.SaveAsync(_route.Id, new ScheduleChanges(TripDirection.Outbound, [change], []), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveAsync_ExistingTripKeepsItsCode()
    {
        var existing = ExistingTrip();
        var trip = Copy(existing);
        trip.Code = "promijenjeno";

        await _service.SaveAsync(
            _route.Id,
            new ScheduleChanges(TripDirection.Outbound, [new ScheduleTripChange(trip, StopTimes(8 * 3600, 8 * 3600 + 300), Loaded)], []),
            TestContext.Current.CancellationToken);

        Assert.Equal("T1", Assert.Single(_schedule.Saved).Trip.Code);
    }

    [Fact]
    public async Task SaveAsync_DeletesTripOfThisRoute()
    {
        var existing = ExistingTrip();

        await _service.SaveAsync(
            _route.Id,
            new ScheduleChanges(TripDirection.Outbound, [], [new TripVersion(existing.Id, Loaded)]),
            TestContext.Current.CancellationToken);

        Assert.Equal([existing.Id], _schedule.Deleted);
    }

    [Fact]
    public async Task SaveAsync_TripOfAnotherRoute_IsConflict()
    {
        var existing = ExistingTrip();
        existing.RouteId = Guid.NewGuid();

        await Assert.ThrowsAsync<ConflictException>(() => _service.SaveAsync(
            _route.Id,
            new ScheduleChanges(TripDirection.Outbound, [], [new TripVersion(existing.Id, Loaded)]),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveAsync_UnknownCalendar_IsRejected()
    {
        var trip = NewTrip(_workdays);
        trip.ServiceCalendarId = Guid.NewGuid();

        await Assert.ThrowsAsync<ValidationException>(() => _service.SaveAsync(
            _route.Id,
            new ScheduleChanges(TripDirection.Outbound, [new ScheduleTripChange(trip, StopTimes(3600, 3900), null)], []),
            TestContext.Current.CancellationToken));
    }

    private void AddTrip(ServiceCalendarModel calendar, StopModel[] stops, int start)
    {
        var trip = NewTrip(calendar);
        trip.Id = Guid.NewGuid();
        trip.RouteId = _route.Id;
        trip.DirectionId = TripDirection.Outbound;
        var times = stops.Select((s, i) => (IStopTimeModel)new StopTimeModel
        {
            TripId = trip.Id,
            StopSequence = i + 1,
            StopId = s.Id,
            ArrivalTime = start + i * 120,
            DepartureTime = start + i * 120
        }).ToList();
        _timetable.Trips.Add(new TripStopTimes(trip, times));
    }

    private TripModel ExistingTrip()
    {
        var trip = NewTrip(_workdays);
        trip.Id = Guid.NewGuid();
        trip.Code = "T1";
        trip.RouteId = _route.Id;
        trip.UpdatedAt = Loaded;
        _schedule.Trips[trip.Id] = trip;
        return trip;
    }

    private static TripModel Copy(TripModel trip) => new()
    {
        Id = trip.Id,
        Code = trip.Code,
        ServiceCalendarId = trip.ServiceCalendarId,
        Headsign = trip.Headsign
    };

    private static TripModel NewTrip(ServiceCalendarModel calendar) => new()
    {
        ServiceCalendarId = calendar.Id,
        Headsign = "Sopot"
    };

    private IReadOnlyList<IStopTimeModel> StopTimes(params int[] times) =>
        times.Select((t, i) => (IStopTimeModel)new StopTimeModel
        {
            StopId = _stops[i].Id,
            ArrivalTime = t,
            DepartureTime = t,
            Timepoint = true
        }).ToList();

    private static StopModel Stop(string code) => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        Name = code,
        LocationType = LocationType.StopOrPlatform
    };
}
