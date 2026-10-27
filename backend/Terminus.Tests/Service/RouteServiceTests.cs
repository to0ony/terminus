using Terminus.Common.Exceptions;
using Terminus.Model;
using Terminus.Model.Common.Enums;
using Terminus.Service;
using Terminus.Tests.Fakes;

namespace Terminus.Tests.Service;

public class RouteServiceTests
{
    private static readonly AgencyModel Agency = new()
    {
        Id = Guid.NewGuid(),
        Name = "Test prijevoznik",
        Url = "https://example.com",
        Timezone = "Europe/Zagreb",
        Lang = "hr"
    };

    private readonly FakeRouteRepository _routes = new();
    private readonly RouteService _service;

    public RouteServiceTests()
    {
        _service = new RouteService(_routes, new FakeAgencyRepository(Agency));
    }

    [Fact]
    public async Task CreateAsync_UnknownAgency_ThrowsValidationErrorOnAgencyId()
    {
        var route = ValidRoute();
        route.AgencyId = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(route, TestContext.Current.CancellationToken));

        Assert.Contains(nameof(RouteModel.AgencyId), ex.Errors.Keys);
        Assert.Empty(_routes.Routes);
    }

    [Fact]
    public async Task CreateAsync_KnownAgency_SavesRouteWithAgency()
    {
        var created = await _service.CreateAsync(ValidRoute(), TestContext.Current.CancellationToken);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(Agency.Id, created.AgencyId);
        Assert.Single(_routes.Routes);
    }

    [Fact]
    public async Task CreateAsync_NormalizesCodeAndColors()
    {
        var route = ValidRoute();
        route.Code = "  OS-DJ  ";
        route.Color = "#1565c0";
        route.TextColor = " ffffff ";
        route.Description = "   ";

        var created = await _service.CreateAsync(route, TestContext.Current.CancellationToken);

        Assert.Equal("OS-DJ", created.Code);
        Assert.Equal("1565C0", created.Color);
        Assert.Equal("FFFFFF", created.TextColor);
        Assert.Null(created.Description);
    }

    [Fact]
    public async Task CreateAsync_WithoutShortAndLongName_ThrowsValidationError()
    {
        var route = ValidRoute();
        route.ShortName = " ";
        route.LongName = null;

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(route, TestContext.Current.CancellationToken));

        Assert.Contains(nameof(RouteModel.ShortName), ex.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_InvalidColor_ThrowsValidationError()
    {
        var route = ValidRoute();
        route.Color = "plava";

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(route, TestContext.Current.CancellationToken));

        Assert.Contains(nameof(RouteModel.Color), ex.Errors.Keys);
    }

    [Theory]
    [InlineData((short)3)]
    [InlineData((short)700)]
    [InlineData((short)200)]
    public async Task CreateAsync_BasicOrExtendedRouteType_IsAccepted(short routeType)
    {
        var route = ValidRoute();
        route.RouteType = (RouteType)routeType;

        var created = await _service.CreateAsync(route, TestContext.Current.CancellationToken);

        Assert.Equal(routeType, (short)created.RouteType);
    }

    [Theory]
    [InlineData((short)8)]
    [InlineData((short)99)]
    [InlineData((short)1800)]
    public async Task CreateAsync_UnknownRouteType_ThrowsValidationError(short routeType)
    {
        var route = ValidRoute();
        route.RouteType = (RouteType)routeType;

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(route, TestContext.Current.CancellationToken));

        Assert.Contains(nameof(RouteModel.RouteType), ex.Errors.Keys);
    }

    [Fact]
    public async Task UpdateAsync_UnknownRoute_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.UpdateAsync(Guid.NewGuid(), ValidRoute(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetActiveAsync_CountsOnlyChangedRoutes()
    {
        var active = await _service.CreateAsync(ValidRoute(), TestContext.Current.CancellationToken);
        var inactive = ValidRoute();
        inactive.Code = "OS-VK";
        inactive.IsActive = false;
        inactive = (RouteModel)await _service.CreateAsync(inactive, TestContext.Current.CancellationToken);

        var changed = await _service.SetActiveAsync([active.Id, inactive.Id], false, TestContext.Current.CancellationToken);

        Assert.Equal(1, changed);
        Assert.All(_routes.Routes, r => Assert.False(r.IsActive));
    }

    [Fact]
    public async Task SetAgencyAsync_UnknownAgency_ThrowsValidationError()
    {
        var route = await _service.CreateAsync(ValidRoute(), TestContext.Current.CancellationToken);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.SetAgencyAsync([route.Id], Guid.NewGuid(), TestContext.Current.CancellationToken));

        Assert.Contains("AgencyId", ex.Errors.Keys);
        Assert.Equal(Agency.Id, route.AgencyId);
    }

    [Fact]
    public async Task SetActiveAsync_UnknownRoute_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.SetActiveAsync([Guid.NewGuid()], true, TestContext.Current.CancellationToken));
    }

    private static RouteModel ValidRoute() => new()
    {
        AgencyId = Agency.Id,
        Code = "OS-DJ",
        ShortName = "12",
        LongName = "Osijek - Đakovo"
    };
}
