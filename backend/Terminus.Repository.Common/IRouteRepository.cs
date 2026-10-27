using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;

namespace Terminus.Repository.Common;

public interface IRouteRepository
{
    Task<IRouteModel?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<IRouteModel>> FindAsync(RouteFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Kao FindAsync, uz prijevoznika, broj varijanti, vožnji i stanica te prvi polazak i zadnji dolazak.</summary>
    Task<PagedResult<RouteTableRow>> FindTableAsync(RouteFilter filter, CancellationToken cancellationToken = default);

    Task<IRouteModel> InsertAsync(IRouteModel route, CancellationToken cancellationToken = default);

    /// <returns>Ažurirana linija ili null ako ne postoji.</returns>
    Task<IRouteModel?> UpdateAsync(IRouteModel route, CancellationToken cancellationToken = default);

    /// <returns>false ako linija ne postoji.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
