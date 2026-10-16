using Terminus.Common.Exceptions;
using Terminus.Model;
using Terminus.Service;
using Terminus.Tests.Fakes;

namespace Terminus.Tests.Service;

public class FareServiceTests
{
    private static readonly AgencyModel Agency = new()
    {
        Id = Guid.NewGuid(),
        Code = "PR",
        Name = "Prijevoznik",
        Url = "https://example.com",
        Timezone = "Europe/Zagreb",
        Lang = "hr"
    };

    private readonly FakeFareRepository _fares = new();
    private readonly FakeRouteRepository _routes = new();
    private readonly FareService _service;

    public FareServiceTests()
    {
        _service = new FareService(_fares, new FakeAgencyRepository(Agency), _routes);
    }

    [Fact]
    public async Task CreateAsync_NormalizesCurrency()
    {
        var fare = ValidFare();
        fare.CurrencyType = " eur ";

        var created = await _service.CreateAsync(fare, TestContext.Current.CancellationToken);

        Assert.Equal("EUR", created.CurrencyType);
    }

    [Fact]
    public async Task CreateAsync_InvalidCurrency_ThrowsValidationError()
    {
        var fare = ValidFare();
        fare.CurrencyType = "EURO";

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(fare, TestContext.Current.CancellationToken));

        Assert.Contains(nameof(FareModel.CurrencyType), ex.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_NegativePrice_ThrowsValidationError()
    {
        var fare = ValidFare();
        fare.Price = -1;

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(fare, TestContext.Current.CancellationToken));

        Assert.Contains(nameof(FareModel.Price), ex.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_EmptyRule_ThrowsValidationError()
    {
        var fare = ValidFare();
        fare.Rules = [new FareRuleModel { OriginId = " " }];

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(fare, TestContext.Current.CancellationToken));

        Assert.Contains("Rules[0]", ex.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_UnknownRoute_ThrowsValidationError()
    {
        var fare = ValidFare();
        fare.Rules = [new FareRuleModel { RouteId = Guid.NewGuid() }];

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(fare, TestContext.Current.CancellationToken));

        Assert.Contains("Rules[0].RouteId", ex.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_UnknownAgency_ThrowsValidationError()
    {
        var fare = ValidFare();
        fare.AgencyId = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(fare, TestContext.Current.CancellationToken));

        Assert.Contains(nameof(FareModel.AgencyId), ex.Errors.Keys);
    }

    private static FareModel ValidFare() => new()
    {
        Code = "F1",
        Price = 1.5m,
        CurrencyType = "EUR",
        AgencyId = Agency.Id
    };
}
