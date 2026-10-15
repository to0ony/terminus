using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;

namespace Terminus.Service.Common;

public interface IAgencyService
{
    Task<IAgencyModel> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<IAgencyModel>> FindAsync(AgencyFilter filter, CancellationToken cancellationToken = default);

    Task<IAgencyModel> CreateAsync(IAgencyModel agency, CancellationToken cancellationToken = default);

    Task<IAgencyModel> UpdateAsync(Guid id, IAgencyModel agency, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
