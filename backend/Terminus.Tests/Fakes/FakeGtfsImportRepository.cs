using Terminus.Model.Common;
using Terminus.Repository.Common;

namespace Terminus.Tests.Fakes;

/// <summary>Pamti zadnji upisani feed i varijante.</summary>
internal sealed class FakeGtfsImportRepository : IGtfsImportRepository
{
    public GtfsFeedData? Feed { get; private set; }
    public IReadOnlyList<IRouteVariantModel> Variants { get; private set; } = [];
    public GtfsExistingCodes Existing { get; set; } = new(
        new HashSet<string>(), new HashSet<string>(), new HashSet<string>(), new HashSet<string>());

    public Task<GtfsExistingCodes> GetExistingCodesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Existing);

    public Task MergeAsync(GtfsFeedData feed, IReadOnlyList<IRouteVariantModel> variants, CancellationToken cancellationToken = default)
    {
        Feed = feed;
        Variants = variants;
        return Task.CompletedTask;
    }
}

/// <summary>Vraća zadane brojeve postojećih podataka.</summary>
internal sealed class FakeSetupRepository : ISetupRepository
{
    public TimetableCounts Counts { get; set; } = new(0, 0, 0, 0);

    public Task<TimetableCounts> GetCountsAsync(CancellationToken cancellationToken = default) => Task.FromResult(Counts);
}
