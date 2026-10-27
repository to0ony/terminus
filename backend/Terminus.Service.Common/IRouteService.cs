using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;

namespace Terminus.Service.Common;

public interface IRouteService
{
    Task<IRouteModel> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<IRouteModel>> FindAsync(RouteFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Popis linija za tablicu: uz svaku prijevoznik, broj varijanti, vožnji i stanica te raspon vremena.</summary>
    Task<PagedResult<RouteTableRow>> FindTableAsync(RouteFilter filter, CancellationToken cancellationToken = default);

    Task<IRouteModel> CreateAsync(IRouteModel route, CancellationToken cancellationToken = default);

    Task<IRouteModel> UpdateAsync(Guid id, IRouteModel route, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Aktivira ili deaktivira više linija odjednom.</summary>
    /// <returns>Broj linija kojima se stanje promijenilo.</returns>
    Task<int> SetActiveAsync(IReadOnlyCollection<Guid> ids, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>Premješta više linija drugom prijevozniku; vožnje i vozni red ostaju.</summary>
    /// <returns>Broj premještenih linija.</returns>
    Task<int> SetAgencyAsync(IReadOnlyCollection<Guid> ids, Guid agencyId, CancellationToken cancellationToken = default);
}
