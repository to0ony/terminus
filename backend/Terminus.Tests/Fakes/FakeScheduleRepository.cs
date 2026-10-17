using Terminus.Model.Common;
using Terminus.Repository.Common;

namespace Terminus.Tests.Fakes;

/// <summary>Vožnje u memoriji; zadnje spremanje je u <see cref="Saved"/> i <see cref="Deleted"/>.</summary>
internal sealed class FakeScheduleRepository : IScheduleRepository
{
    public Dictionary<Guid, ITripModel> Trips { get; } = [];
    public IReadOnlyList<TripStopTimes> Saved { get; private set; } = [];
    public IReadOnlyCollection<Guid> Deleted { get; private set; } = [];

    public Task<IReadOnlyDictionary<Guid, ITripModel>> GetTripsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, ITripModel>>(
            Trips.Where(kv => ids.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value));

    public Task SaveAsync(IReadOnlyList<TripStopTimes> upserts, IReadOnlyCollection<Guid> deletes, CancellationToken cancellationToken = default)
    {
        Saved = upserts;
        Deleted = deletes;
        return Task.CompletedTask;
    }
}
