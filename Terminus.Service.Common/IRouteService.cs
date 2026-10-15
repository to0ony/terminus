using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;

namespace Terminus.Service.Common;

public interface IRouteService
{
    Task<IRouteModel> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<IRouteModel>> FindAsync(RouteFilter filter, CancellationToken cancellationToken = default);

    Task<IRouteModel> CreateAsync(IRouteModel route, CancellationToken cancellationToken = default);

    Task<IRouteModel> UpdateAsync(Guid id, IRouteModel route, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
