using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;

namespace Terminus.Service.Common;

public interface IRouteService
{
    Task<IRouteModel> GetAsync(Guid agencyId, Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<IRouteModel>> FindAsync(Guid agencyId, RouteFilter filter, CancellationToken cancellationToken = default);

    Task<IRouteModel> CreateAsync(Guid agencyId, IRouteModel route, CancellationToken cancellationToken = default);

    Task<IRouteModel> UpdateAsync(Guid agencyId, Guid id, IRouteModel route, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid agencyId, Guid id, CancellationToken cancellationToken = default);
}
