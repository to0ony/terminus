using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;
using Terminus.Repository.Common;

namespace Terminus.Tests.Fakes;

/// <summary>Stanice u memoriji; dovoljno za dohvat, stvaranje i izmjenu.</summary>
internal sealed class FakeStopRepository(IEnumerable<IStopModel> stops) : IStopRepository
{
    private readonly List<IStopModel> _stops = [.. stops];

    /// <summary>Stanice na kojima staju vožnje.</summary>
    public HashSet<Guid> UsedByTrips { get; } = [];

    public Task<IStopModel?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_stops.FirstOrDefault(s => s.Id == id));

    public Task<IStopModel?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        Task.FromResult(_stops.FirstOrDefault(s => s.Code == code));

    public Task<IReadOnlyList<IStopModel>> GetManyAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IStopModel>>(_stops.Where(s => ids.Contains(s.Id)).ToList());

    public Task<PagedResult<IStopModel>> FindAsync(StopFilter filter, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<bool> HasChildStopsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_stops.Any(s => s.ParentStationId == id));

    public Task<bool> IsUsedByTripsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(UsedByTrips.Contains(id));

    public Task<IStopModel> InsertAsync(IStopModel stop, CancellationToken cancellationToken = default)
    {
        stop.Id = Guid.NewGuid();
        _stops.Add(stop);
        return Task.FromResult(stop);
    }

    public Task<IStopModel?> UpdateAsync(IStopModel stop, CancellationToken cancellationToken = default)
    {
        var index = _stops.FindIndex(s => s.Id == stop.Id);
        if (index < 0)
        {
            return Task.FromResult<IStopModel?>(null);
        }

        _stops[index] = stop;
        return Task.FromResult<IStopModel?>(stop);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
