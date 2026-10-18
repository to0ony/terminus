using Terminus.Common.Exceptions;
using Terminus.Model;
using Terminus.Service;
using Terminus.Tests.Fakes;

namespace Terminus.Tests.Service;

public class AgencyServiceTests
{
    [Fact]
    public async Task CreateAsync_DifferentTimezoneThanExisting_ThrowsValidationError()
    {
        var service = new AgencyService(new FakeAgencyRepository(Agency("Europe/Zagreb")));

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateAsync(Agency("Europe/London"), TestContext.Current.CancellationToken));

        Assert.Contains(nameof(AgencyModel.Timezone), ex.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_SameTimezoneAsExisting_CreatesAgency()
    {
        var service = new AgencyService(new FakeAgencyRepository(Agency("Europe/Zagreb")));

        var created = await service.CreateAsync(Agency("Europe/Zagreb"), TestContext.Current.CancellationToken);

        Assert.NotEqual(Guid.Empty, created.Id);
    }

    [Fact]
    public async Task CreateAsync_WithoutTimezone_UsesDefaultZone()
    {
        var service = new AgencyService(new FakeAgencyRepository(Agency("Europe/Zagreb")));

        var created = await service.CreateAsync(Agency(" "), TestContext.Current.CancellationToken);

        Assert.Equal("Europe/Zagreb", created.Timezone);
    }

    [Fact]
    public async Task UpdateAsync_OnlyAgency_CanChangeTimezone()
    {
        var existing = Agency("Europe/Zagreb");
        var service = new AgencyService(new FakeAgencyRepository(existing));

        var updated = await service.UpdateAsync(existing.Id, Agency("Europe/London"), TestContext.Current.CancellationToken);

        Assert.Equal("Europe/London", updated.Timezone);
    }

    [Fact]
    public async Task CreateAsync_WithoutCode_DerivesCodeFromName()
    {
        var service = new AgencyService(new FakeAgencyRepository());
        var agency = Agency("Europe/Zagreb");
        agency.Code = " ";
        agency.Name = "Autotrans Čakovec";

        var created = await service.CreateAsync(agency, TestContext.Current.CancellationToken);

        Assert.False(string.IsNullOrWhiteSpace(created.Code));
    }

    [Fact]
    public async Task UpdateAsync_WithoutCode_ThrowsValidationError()
    {
        var existing = Agency("Europe/Zagreb");
        var service = new AgencyService(new FakeAgencyRepository(existing));
        var agency = Agency("Europe/Zagreb");
        agency.Code = " ";

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.UpdateAsync(existing.Id, agency, TestContext.Current.CancellationToken));

        Assert.Contains(nameof(AgencyModel.Code), ex.Errors.Keys);
    }

    [Fact]
    public async Task DeleteAsync_MoveRoutesToOtherAgency_DeletesAndMovesRoutes()
    {
        var leaving = Agency("Europe/Zagreb");
        var staying = Agency("Europe/Zagreb");
        var repository = new FakeAgencyRepository(leaving, staying);
        var service = new AgencyService(repository);

        await service.DeleteAsync(leaving.Id, staying.Id, TestContext.Current.CancellationToken);

        Assert.Equal((leaving.Id, (Guid?)staying.Id), repository.LastDelete);
    }

    [Fact]
    public async Task DeleteAsync_MoveRoutesToItself_ThrowsValidationError()
    {
        var agency = Agency("Europe/Zagreb");
        var repository = new FakeAgencyRepository(agency);
        var service = new AgencyService(repository);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.DeleteAsync(agency.Id, agency.Id, TestContext.Current.CancellationToken));

        Assert.Contains("moveRoutesTo", ex.Errors.Keys);
        Assert.Null(repository.LastDelete);
    }

    [Fact]
    public async Task DeleteAsync_MoveRoutesToMissingAgency_ThrowsValidationError()
    {
        var agency = Agency("Europe/Zagreb");
        var service = new AgencyService(new FakeAgencyRepository(agency));

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.DeleteAsync(agency.Id, Guid.NewGuid(), TestContext.Current.CancellationToken));

        Assert.Contains("moveRoutesTo", ex.Errors.Keys);
    }

    [Fact]
    public async Task DeleteAsync_MissingAgency_ThrowsNotFound()
    {
        var service = new AgencyService(new FakeAgencyRepository());

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.DeleteAsync(Guid.NewGuid(), cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateAsync_InvalidFareUrl_ThrowsValidationError()
    {
        var service = new AgencyService(new FakeAgencyRepository());
        var agency = Agency("Europe/Zagreb");
        agency.FareUrl = "cijene.html";

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateAsync(agency, TestContext.Current.CancellationToken));

        Assert.Contains(nameof(AgencyModel.FareUrl), ex.Errors.Keys);
    }

    private static AgencyModel Agency(string timezone) => new()
    {
        Id = Guid.NewGuid(),
        Code = "PRIJEVOZNIK",
        Name = "Prijevoznik",
        Url = "https://example.com",
        Timezone = timezone,
        Lang = "hr"
    };
}
