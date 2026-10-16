using Terminus.Common.Exceptions;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Service;
using Terminus.Tests.Fakes;

namespace Terminus.Tests.Service;

public class ShapeServiceTests
{
    private readonly FakeShapeRepository _repository = new();
    private readonly ShapeService _service;

    public ShapeServiceTests()
    {
        _service = new ShapeService(_repository);
    }

    [Fact]
    public async Task CreateAsync_NumbersPointsInOrder()
    {
        var created = await _service.CreateAsync(Shape(Point(45.5, 18.6), Point(45.4, 18.5), Point(45.3, 18.4)),
            TestContext.Current.CancellationToken);

        Assert.Equal([1, 2, 3], created.Points.Select(p => p.Sequence));
        Assert.Equal(3, created.PointCount);
    }

    [Fact]
    public async Task CreateAsync_SinglePoint_ThrowsValidationError()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(Shape(Point(45.5, 18.6)), TestContext.Current.CancellationToken));

        Assert.Contains(nameof(ShapeModel.Points), ex.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_DecreasingDistance_ThrowsValidationError()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(Shape(Point(45.5, 18.6, 100), Point(45.4, 18.5, 50)), TestContext.Current.CancellationToken));

        Assert.Contains("Points[1].DistTraveled", ex.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_InvalidLatitude_ThrowsValidationError()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(Shape(Point(95, 18.6), Point(45.4, 18.5)), TestContext.Current.CancellationToken));

        Assert.Contains("Points[0].Latitude", ex.Errors.Keys);
    }

    private static ShapeModel Shape(params IShapePointModel[] points) => new() { Code = "SH-1", Points = points };

    private static ShapePointModel Point(double latitude, double longitude, double? distance = null) =>
        new() { Latitude = latitude, Longitude = longitude, DistTraveled = distance };
}
