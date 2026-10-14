using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;

namespace Terminus.Repository.Common;

public interface IStopRepository
{
    Task<IStopModel?> GetAsync(Guid agencyId, Guid id, CancellationToken cancellationToken = default);

    /// <returns>Stanice agencije s traženim Id-evima; nepostojeći Id-evi se izostavljaju.</returns>
    Task<IReadOnlyList<IStopModel>> GetManyAsync(Guid agencyId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    Task<PagedResult<IStopModel>> FindAsync(Guid agencyId, StopFilter filter, CancellationToken cancellationToken = default);

    Task<bool> HasChildStopsAsync(Guid agencyId, Guid id, CancellationToken cancellationToken = default);

    Task<IStopModel> InsertAsync(IStopModel stop, CancellationToken cancellationToken = default);

    /// <returns>Ažurirana stanica ili null ako ne postoji.</returns>
    Task<IStopModel?> UpdateAsync(IStopModel stop, CancellationToken cancellationToken = default);

    /// <returns>false ako stanica ne postoji.</returns>
    Task<bool> DeleteAsync(Guid agencyId, Guid id, CancellationToken cancellationToken = default);
}
