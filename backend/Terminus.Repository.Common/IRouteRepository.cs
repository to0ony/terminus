using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;

namespace Terminus.Repository.Common;

public interface IRouteRepository
{
    Task<IRouteModel?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<IRouteModel>> FindAsync(RouteFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Kao FindAsync, uz prijevoznika, broj varijanti i razdoblje voznog reda.</summary>
    Task<PagedResult<RouteTableRow>> FindTableAsync(RouteFilter filter, CancellationToken cancellationToken = default);

    Task<IRouteModel> InsertAsync(IRouteModel route, CancellationToken cancellationToken = default);

    /// <returns>Ažurirana linija ili null ako ne postoji.</returns>
    Task<IRouteModel?> UpdateAsync(IRouteModel route, CancellationToken cancellationToken = default);

    /// <returns>Linije s traženim Id-evima; nepostojeći Id-evi se izostavljaju.</returns>
    Task<IReadOnlyList<IRouteModel>> GetManyAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>Aktivira ili deaktivira linije odjednom.</summary>
    /// <returns>Broj izmijenjenih linija.</returns>
    Task<int> SetActiveAsync(IReadOnlyCollection<Guid> ids, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>Premješta linije drugom prijevozniku; vožnje i vozni red ostaju.</summary>
    /// <returns>Broj izmijenjenih linija.</returns>
    Task<int> SetAgencyAsync(IReadOnlyCollection<Guid> ids, Guid agencyId, CancellationToken cancellationToken = default);

    /// <returns>false ako linija ne postoji.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
