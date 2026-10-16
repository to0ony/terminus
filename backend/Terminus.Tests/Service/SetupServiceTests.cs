using Terminus.Common.Exceptions;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Repository.Common;
using Terminus.Service;
using Terminus.Tests.Fakes;

namespace Terminus.Tests.Service;

public class SetupServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeAgencyRepository _agencies = new();
    private readonly FakeSettingsRepository _settings = new();

    private SetupService Service => new(
        new CountingSetupRepository(_agencies),
        _settings,
        new AgencyService(_agencies),
        new FixedTimeProvider(Now));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetStatusAsync_NewInstance_IsNotCompleted()
    {
        var status = await Service.GetStatusAsync(Ct);

        Assert.False(status.Completed);
        Assert.Null(status.CompletedAt);
        Assert.Equal(0, status.Counts.Agencies);
    }

    [Fact]
    public async Task StartBlankAsync_CreatesAgencyWithDefaultsAndCompletesSetup()
    {
        var agency = await Service.StartBlankAsync(new AgencyModel { Name = "Gradski prijevoz putnika", Url = "https://www.gpp.hr" }, Ct);

        Assert.Equal("gradski-prijevoz-putnika", agency.Code);
        Assert.Equal("Europe/Zagreb", agency.Timezone);
        Assert.Equal("hr", agency.Lang);

        var status = await Service.GetStatusAsync(Ct);
        Assert.True(status.Completed);
        Assert.Equal(Now.UtcDateTime, status.CompletedAt);
        Assert.Equal(1, status.Counts.Agencies);
    }

    [Fact]
    public async Task StartBlankAsync_KeepsGivenCode()
    {
        var agency = await Service.StartBlankAsync(new AgencyModel { Code = "GPP", Name = "GPP", Url = "https://www.gpp.hr" }, Ct);

        Assert.Equal("GPP", agency.Code);
    }

    [Fact]
    public async Task StartBlankAsync_InvalidAgency_LeavesSetupOpen()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            Service.StartBlankAsync(new AgencyModel { Name = "GPP", Url = "nije-url" }, Ct));

        Assert.False((await Service.GetStatusAsync(Ct)).Completed);
    }

    [Fact]
    public async Task StartBlankAsync_AlreadyCompleted_ThrowsConflict()
    {
        await Service.StartBlankAsync(new AgencyModel { Name = "GPP", Url = "https://www.gpp.hr" }, Ct);

        await Assert.ThrowsAsync<ConflictException>(() =>
            Service.StartBlankAsync(new AgencyModel { Name = "Drugi", Url = "https://example.com" }, Ct));
    }

    /// <summary>Broji prijevoznike iz lažnog repozitorija; ostalog voznog reda nema.</summary>
    private sealed class CountingSetupRepository(FakeAgencyRepository agencies) : ISetupRepository
    {
        public async Task<TimetableCounts> GetCountsAsync(CancellationToken cancellationToken = default) =>
            new(await agencies.AnyAsync(cancellationToken) ? 1 : 0, 0, 0, 0);
    }
}
