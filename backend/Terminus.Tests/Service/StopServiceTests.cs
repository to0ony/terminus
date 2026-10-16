using Terminus.Common.Exceptions;
using Terminus.Model;
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
