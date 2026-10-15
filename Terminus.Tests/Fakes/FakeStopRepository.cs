using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;
using Terminus.Repository.Common;

namespace Terminus.Tests.Fakes;

/// <summary>Stanice u memoriji; dovoljno za dohvat po Id-u i kodu.</summary>
internal sealed class FakeStopRepository(IEnumerable<IStopModel> stops) : IStopRepository
{
    private readonly List<IStopModel> _stops = [.. stops];

    public Task<IStopModel?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_stops.FirstOrDefault(s => s.Id == id));

    public Task<IStopModel?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        Task.FromResult(_stops.FirstOrDefault(s => s.Code == code));

    public Task<IReadOnlyList<IStopModel>> GetManyAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IStopModel>>(_stops.Where(s => ids.Contains(s.Id)).ToList());

    public Task<PagedResult<IStopModel>> FindAsync(StopFilter filter, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<bool> HasChildStopsAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<IStopModel> InsertAsync(IStopModel stop, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<IStopModel?> UpdateAsync(IStopModel stop, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
