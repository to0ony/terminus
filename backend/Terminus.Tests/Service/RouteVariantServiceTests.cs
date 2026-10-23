using Terminus.Common.Exceptions;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Service;
using Terminus.Tests.Fakes;

namespace Terminus.Tests.Service;

public class RouteVariantServiceTests
{
    private readonly RouteModel _route = new() { Id = Guid.NewGuid(), Code = "6", ShortName = "6" };
    private readonly StopModel[] _stops = [Stop("Črnomerec"), Stop("Trg"), Stop("Savski most"), Stop("Sopot")];
    private readonly StopModel _station = Stop("Kolodvor", LocationType.Station);

    private readonly FakeRouteVariantRepository _variants = new();
    private readonly FakeTimetableRepository _timetable = new();
    private readonly RouteVariantService _service;

    public RouteVariantServiceTests()
    {
        var routes = new FakeRouteRepository();
        routes.Routes.Add(_route);
        _service = new RouteVariantService(
            _variants, routes, new FakeStopRepository([.. _stops, _station]), new FakeShapeRepository(), _timetable);
    }

    [Fact]
    public async Task CreateAsync_FirstStopHasNoTravelTimeAndVariantGoesLast()
    {
        await _service.CreateAsync(_route.Id, Variant("Glavna", 0, 1, 3), TestContext.Current.CancellationToken);

        var created = await _service.CreateAsync(_route.Id, Variant("Skraćena", 0, 1, 2), TestContext.Current.CancellationToken);

        Assert.Equal(0, created.Variant.Stops[0].TravelTime);
        Assert.Equal(1, created.Variant.SortOrder);
        Assert.Equal(VariantSource.Manual, created.Variant.Source);
        Assert.Equal("Trg", created.Stops[_stops[1].Id].Name);
    }

    [Fact]
    public async Task CreateAsync_StationInsteadOfPlatform_IsRejected()
    {
        var variant = Variant("Glavna", 0, 1);
        variant.Stops = [.. variant.Stops, new VariantStop(_station.Id, 120, 0)];

        var error = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreateAsync(_route.Id, variant, TestContext.Current.CancellationToken));

        Assert.Contains("Stops[2].StopId", error.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_OneStop_IsRejected()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreateAsync(_route.Id, Variant("Glavna", 0), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateAsync_InactiveStopInMiddle_IsAllowed()
    {
        _stops[1].IsActive = false;

        var created = await _service.CreateAsync(_route.Id, Variant("Glavna", 0, 1, 2, 3), TestContext.Current.CancellationToken);

        Assert.Equal(4, created.Variant.Stops.Count);
    }

    [Theory]
    [InlineData(0, "počinjati")]
    [InlineData(3, "završavati")]
    public async Task CreateAsync_InactiveFirstOrLastStop_IsRejected(int inactive, string expected)
    {
        _stops[inactive].IsActive = false;

        var error = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreateAsync(_route.Id, Variant("Glavna", 0, 1, 2, 3), TestContext.Current.CancellationToken));

        Assert.Contains(expected, Assert.Single(error.Errors["Stops"]));
    }

    [Fact]
    public async Task CreateAsync_InactiveStopLeavingTwo_IsRejected()
    {
        _stops[1].IsActive = false;

        var error = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreateAsync(_route.Id, Variant("Glavna", 0, 1, 2), TestContext.Current.CancellationToken));

        Assert.Contains("Stops", error.Errors.Keys);
    }

    [Fact]
    public async Task UpdateAsync_ChangingStopsOfVariantWithTrips_IsConflict()
    {
        var created = await _service.CreateAsync(_route.Id, Variant("Glavna", 0, 1, 3), TestContext.Current.CancellationToken);
        _variants.TripCounts[created.Variant.Id] = 40;

        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.UpdateAsync(created.Variant.Id, Variant("Glavna", 0, 2, 3), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateAsync_TravelTimesOfVariantWithTrips_AreAllowed()
    {
        var created = await _service.CreateAsync(_route.Id, Variant("Glavna", 0, 1, 3), TestContext.Current.CancellationToken);
        _variants.TripCounts[created.Variant.Id] = 40;
        var changed = Variant("Glavna 6", 0, 1, 3);
        changed.Stops = changed.Stops.Select(s => s with { TravelTime = s.TravelTime + 60 }).ToList();

        var updated = await _service.UpdateAsync(created.Variant.Id, changed, TestContext.Current.CancellationToken);

        Assert.Equal("Glavna 6", updated.Variant.Name);
        Assert.Equal(180, updated.Variant.Stops[1].TravelTime);
    }

    [Fact]
    public async Task DeleteAsync_VariantWithTrips_IsConflict()
    {
        var created = await _service.CreateAsync(_route.Id, Variant("Glavna", 0, 1), TestContext.Current.CancellationToken);
        _variants.TripCounts[created.Variant.Id] = 1;

        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.DeleteAsync(created.Variant.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeriveAsync_GroupsTripsByDirectionAndStopsWithMedianTimes()
    {
        AddTrip(TripDirection.Outbound, [0, 1, 3], [0, 120, 600], "Sopot");
        AddTrip(TripDirection.Outbound, [0, 1, 3], [0, 180, 660], "Sopot");
        AddTrip(TripDirection.Outbound, [0, 1, 3], [0, 180, 700], "Sopot");
        AddTrip(TripDirection.Outbound, [0, 1, 2], [0, 120, 300], "Savski most");
        AddTrip(TripDirection.Inbound, [3, 1, 0], [0, 400, 520], "Črnomerec");

        var result = await _service.DeriveAsync(_route.Id, TestContext.Current.CancellationToken);

        Assert.Equal(3, result.Variants.Count);
        var main = result.Variants[0];
        Assert.Equal("Črnomerec → Sopot", main.Name);
        Assert.Equal(0, main.SortOrder);
        Assert.Equal("Sopot", main.Headsign);
        Assert.Equal([0, 180, 480], main.Stops.Select(s => s.TravelTime));
        Assert.Equal(VariantSource.Gtfs, main.Source);
        Assert.Equal(1, result.Variants[1].SortOrder);
        Assert.Equal(TripDirection.Inbound, result.Variants[2].DirectionId);
        Assert.Equal(5, _variants.TripVariants.Count);
    }

    [Fact]
    public async Task DeriveAsync_RouteWithVariants_IsConflict()
    {
        await _service.CreateAsync(_route.Id, Variant("Glavna", 0, 1), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ConflictException>(() => _service.DeriveAsync(_route.Id, TestContext.Current.CancellationToken));
    }

    private RouteVariantModel Variant(string name, params int[] stops) => new()
    {
        Name = name,
        DirectionId = TripDirection.Outbound,
        Stops = stops.Select((index, i) => new VariantStop(_stops[index].Id, i == 0 ? 30 : 120, 0)).ToList()
    };

    private void AddTrip(TripDirection direction, int[] stops, int[] offsets, string headsign)
    {
        var trip = new TripModel
        {
            Id = Guid.NewGuid(),
            RouteId = _route.Id,
            DirectionId = direction,
            Headsign = headsign
        };
        var times = stops.Select((index, i) => (IStopTimeModel)new StopTimeModel
        {
            TripId = trip.Id,
            StopSequence = i + 1,
            StopId = _stops[index].Id,
            ArrivalTime = 7 * 3600 + offsets[i],
            DepartureTime = 7 * 3600 + offsets[i]
        }).ToList();
        _timetable.Trips.Add(new TripStopTimes(trip, times));
    }

    private static StopModel Stop(string name, LocationType type = LocationType.StopOrPlatform) => new()
    {
        Id = Guid.NewGuid(),
        Code = name,
        Name = name,
        LocationType = type
    };
}
