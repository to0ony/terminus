using Terminus.Model.Common;
using Terminus.Repository.Common;

namespace Terminus.Tests.Fakes;

/// <summary>Pamti zadnji upisani feed.</summary>
internal sealed class FakeGtfsImportRepository : IGtfsImportRepository
{
    public GtfsFeedData? Feed { get; private set; }

    public Task ReplaceAllAsync(GtfsFeedData feed, CancellationToken cancellationToken = default)
    {
        Feed = feed;
        return Task.CompletedTask;
    }
}

/// <summary>Vraća zadane brojeve postojećih podataka.</summary>
internal sealed class FakeSetupRepository : ISetupRepository
{
    public TimetableCounts Counts { get; set; } = new(0, 0, 0, 0);

    public Task<TimetableCounts> GetCountsAsync(CancellationToken cancellationToken = default) => Task.FromResult(Counts);
}
