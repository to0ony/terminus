using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;

namespace Terminus.Service.Common;

public interface IStopService
{
    Task<IStopModel> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<IStopModel>> FindAsync(StopFilter filter, CancellationToken cancellationToken = default);

    Task<IStopModel> CreateAsync(IStopModel stop, CancellationToken cancellationToken = default);

    Task<IStopModel> UpdateAsync(Guid id, IStopModel stop, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
