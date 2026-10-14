using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;

namespace Terminus.Service.Common;

public interface IStopService
{
    Task<IStopModel> GetAsync(Guid agencyId, Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<IStopModel>> FindAsync(Guid agencyId, StopFilter filter, CancellationToken cancellationToken = default);

    Task<IStopModel> CreateAsync(Guid agencyId, IStopModel stop, CancellationToken cancellationToken = default);

    Task<IStopModel> UpdateAsync(Guid agencyId, Guid id, IStopModel stop, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid agencyId, Guid id, CancellationToken cancellationToken = default);
}
