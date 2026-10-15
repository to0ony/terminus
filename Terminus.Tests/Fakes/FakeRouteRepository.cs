using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;
using Terminus.Repository.Common;

namespace Terminus.Tests.Fakes;

/// <summary>Linije u memoriji. Bilježi što je servis spremio.</summary>
internal sealed class FakeRouteRepository : IRouteRepository
{
    public List<IRouteModel> Routes { get; } = [];

    public Task<IRouteModel?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Routes.FirstOrDefault(r => r.Id == id));

    public Task<IRouteModel> InsertAsync(IRouteModel route, CancellationToken cancellationToken = default)
    {
        route.Id = Guid.NewGuid();
        Routes.Add(route);
        return Task.FromResult(route);
    }

    public Task<IRouteModel?> UpdateAsync(IRouteModel route, CancellationToken cancellationToken = default)
    {
        var index = Routes.FindIndex(r => r.Id == route.Id);
        if (index < 0)
        {
            return Task.FromResult<IRouteModel?>(null);
        }

        Routes[index] = route;
        return Task.FromResult<IRouteModel?>(route);
    }

    public Task<PagedResult<IRouteModel>> FindAsync(RouteFilter filter, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
