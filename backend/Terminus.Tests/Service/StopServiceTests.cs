using Terminus.Common.Exceptions;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Service;
using Terminus.Tests.Fakes;

namespace Terminus.Tests.Service;

public class StopServiceTests
{
    private readonly StopModel _station = new()
    {
        Id = Guid.NewGuid(), Code = "OS-AK", Name = "Osijek, Autobusni kolodvor",
        Latitude = 45.55, Longitude = 18.69, LocationType = LocationType.Station
    };

    private readonly StopModel _platform;
    private readonly FakeStopRepository _repository;
    private readonly StopService _service;

    public StopServiceTests()
    {
        _platform = new StopModel
        {
            Id = Guid.NewGuid(), Code = "OS-AK-3", Name = "Peron 3",
            Latitude = 45.55, Longitude = 18.69, ParentStationId = _station.Id, PlatformCode = "3"
        };
        _repository = new FakeStopRepository([_station, _platform]);
        _service = new StopService(_repository);
    }

    [Fact]
    public async Task CreateAsync_EntranceOfStation_IsCreated()
    {
        var entrance = Stop(LocationType.Entrance, parent: _station.Id);

        var created = await _service.CreateAsync(entrance, TestContext.Current.CancellationToken);

        Assert.Equal(LocationType.Entrance, created.LocationType);
    }

    [Theory]
    [InlineData(LocationType.Entrance)]
    [InlineData(LocationType.GenericNode)]
    [InlineData(LocationType.BoardingArea)]
    public async Task CreateAsync_ChildTypeWithoutParent_ThrowsValidationError(LocationType type)
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(Stop(type, parent: null), TestContext.Current.CancellationToken));

        Assert.Contains(nameof(StopModel.ParentStationId), ex.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_BoardingAreaOfStation_ThrowsValidationError()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(Stop(LocationType.BoardingArea, parent: _station.Id), TestContext.Current.CancellationToken));

        Assert.Contains(nameof(StopModel.ParentStationId), ex.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_BoardingAreaOfPlatform_WithoutCoordinates_IsCreated()
    {
        var boarding = Stop(LocationType.BoardingArea, parent: _platform.Id);
        boarding.Latitude = null;
        boarding.Longitude = null;

        var created = await _service.CreateAsync(boarding, TestContext.Current.CancellationToken);

        Assert.Null(created.Latitude);
    }

    [Fact]
    public async Task CreateAsync_PlatformWithoutCoordinates_ThrowsValidationError()
    {
        var stop = Stop(LocationType.StopOrPlatform, parent: null);
        stop.Latitude = null;
        stop.Longitude = null;

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(stop, TestContext.Current.CancellationToken));

        Assert.Contains(nameof(StopModel.Latitude), ex.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_OnlyLatitude_ThrowsValidationError()
    {
        var stop = Stop(LocationType.StopOrPlatform, parent: null);
        stop.Longitude = null;

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(stop, TestContext.Current.CancellationToken));

        Assert.Contains(nameof(StopModel.Latitude), ex.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_WindowsTimezoneName_ThrowsValidationError()
    {
        var stop = Stop(LocationType.StopOrPlatform, parent: null);
        stop.Timezone = "Central European Standard Time";

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(stop, TestContext.Current.CancellationToken));

        Assert.Contains(nameof(StopModel.Timezone), ex.Errors.Keys);
    }

    [Fact]
    public async Task UpdateAsync_StationWithPlatforms_CannotChangeType()
    {
        var update = Stop(LocationType.StopOrPlatform, parent: null);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.UpdateAsync(_station.Id, update, TestContext.Current.CancellationToken));

        Assert.Contains(nameof(StopModel.LocationType), ex.Errors.Keys);
    }

    [Fact]
    public async Task UpdateAsync_PlatformUsedByTrips_CannotBecomeEntrance()
    {
        _repository.UsedByTrips.Add(_platform.Id);
        var update = Stop(LocationType.Entrance, parent: _station.Id);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.UpdateAsync(_platform.Id, update, TestContext.Current.CancellationToken));

        Assert.Contains(nameof(StopModel.LocationType), ex.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_NormalizesOptionalFields()
    {
        var stop = Stop(LocationType.StopOrPlatform, parent: null);
        stop.PublicCode = "  1042 ";
        stop.ZoneId = " ";

        var created = await _service.CreateAsync(stop, TestContext.Current.CancellationToken);

        Assert.Equal("1042", created.PublicCode);
        Assert.Null(created.ZoneId);
    }

    [Fact]
    public async Task FindInAreaAsync_ReturnsStopsInsideBounds()
    {
        var result = await _service.FindInAreaAsync(
            new GeoBounds(45.5, 18.6, 45.6, 18.7), isActive: null, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Stops.Count);
        Assert.False(result.Truncated);

        var elsewhere = await _service.FindInAreaAsync(
            new GeoBounds(45.0, 18.0, 45.1, 18.1), isActive: null, TestContext.Current.CancellationToken);
        Assert.Empty(elsewhere.Stops);
    }

    [Fact]
    public async Task FindInAreaAsync_InvertedBounds_IsRejected()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.FindInAreaAsync(
            new GeoBounds(45.6, 18.7, 45.5, 18.6), isActive: null, TestContext.Current.CancellationToken));

        Assert.Equal(["MinLatitude", "MinLongitude"], ex.Errors.Keys.Order());
    }

    [Fact]
    public async Task FindInAreaAsync_TooManyStops_IsTruncated()
    {
        var stops = Enumerable.Range(0, StopService.MaxAreaStops + 5)
            .Select(i => new StopModel { Id = Guid.NewGuid(), Code = $"S{i}", Name = $"S{i}", Latitude = 45.5, Longitude = 18.6 });
        var service = new StopService(new FakeStopRepository(stops));

        var result = await service.FindInAreaAsync(
            new GeoBounds(45, 18, 46, 19), isActive: null, TestContext.Current.CancellationToken);

        Assert.True(result.Truncated);
        Assert.Equal(StopService.MaxAreaStops, result.Stops.Count);
    }

    [Fact]
    public async Task UpdateAsync_DeactivateStation_IsRejected()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.UpdateAsync(_station.Id, Inactive(_station), TestContext.Current.CancellationToken));

        Assert.Contains(nameof(StopModel.IsActive), ex.Errors.Keys);
    }

    [Fact]
    public async Task UpdateAsync_DeactivateMiddleStop_IsAllowed()
    {
        var stops = await AddStopsAsync(4);
        _repository.Sequences.Add(new StopSequenceUse(true, "Glavna", "1", stops.Select(s => s.Id).ToList()));

        var updated = await _service.UpdateAsync(stops[1].Id, Inactive(stops[1]), TestContext.Current.CancellationToken);

        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_DeactivateFirstStopOfVariant_IsRejected()
    {
        var stops = await AddStopsAsync(4);
        _repository.Sequences.Add(new StopSequenceUse(true, "Glavna", "1", stops.Select(s => s.Id).ToList()));

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.UpdateAsync(stops[0].Id, Inactive(stops[0]), TestContext.Current.CancellationToken));

        var message = Assert.Single(ex.Errors[nameof(StopModel.IsActive)]);
        Assert.Contains("prva", message);
        Assert.Contains("Glavna (linija 1)", message);
    }

    [Fact]
    public async Task UpdateAsync_DeactivateLastStopOfTrip_IsRejected()
    {
        var stops = await AddStopsAsync(4);
        _repository.Sequences.Add(new StopSequenceUse(false, "T-0715", "1", stops.Select(s => s.Id).ToList()));

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.UpdateAsync(stops[3].Id, Inactive(stops[3]), TestContext.Current.CancellationToken));

        var message = Assert.Single(ex.Errors[nameof(StopModel.IsActive)]);
        Assert.Contains("zadnja", message);
        Assert.Contains("T-0715", message);
    }

    [Fact]
    public async Task UpdateAsync_DeactivateLeavingTwoStops_IsRejected()
    {
        var stops = await AddStopsAsync(3);
        _repository.Sequences.Add(new StopSequenceUse(true, "Kratka", "1", stops.Select(s => s.Id).ToList()));

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.UpdateAsync(stops[1].Id, Inactive(stops[1]), TestContext.Current.CancellationToken));

        Assert.Contains(nameof(StopModel.IsActive), ex.Errors.Keys);
    }

    [Fact]
    public async Task UpdateAsync_DeactivateCountsAlreadyInactiveStops()
    {
        var stops = await AddStopsAsync(4);
        stops[1].IsActive = false;
        _repository.Sequences.Add(new StopSequenceUse(true, "Glavna", "1", stops.Select(s => s.Id).ToList()));

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.UpdateAsync(stops[2].Id, Inactive(stops[2]), TestContext.Current.CancellationToken));

        Assert.Contains(nameof(StopModel.IsActive), ex.Errors.Keys);
    }

    private async Task<List<IStopModel>> AddStopsAsync(int count)
    {
        var stops = new List<IStopModel>();
        for (var i = 0; i < count; i++)
        {
            stops.Add(await _repository.InsertAsync(Stop(LocationType.StopOrPlatform, parent: null), TestContext.Current.CancellationToken));
        }
        return stops;
    }

    private static StopModel Inactive(IStopModel stop) => new()
    {
        Code = stop.Code,
        Name = stop.Name,
        Latitude = stop.Latitude,
        Longitude = stop.Longitude,
        LocationType = stop.LocationType,
        ParentStationId = stop.ParentStationId,
        PlatformCode = stop.PlatformCode,
        IsActive = false
    };

    private static StopModel Stop(LocationType type, Guid? parent) => new()
    {
        Code = $"S-{Guid.NewGuid():N}",
        Name = "Nova stanica",
        Latitude = 45.5,
        Longitude = 18.7,
        LocationType = type,
        ParentStationId = parent
    };
}
