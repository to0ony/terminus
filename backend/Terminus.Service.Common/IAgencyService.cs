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

    /// <summary>
    /// Briše prijevoznika. Uz <paramref name="moveRoutesTo"/> njegove linije i cijene prelaze na
    /// tog prijevoznika (npr. kad linije u konzorciju preuzme druga tvrtka).
    /// </summary>
    Task DeleteAsync(Guid id, Guid? moveRoutesTo = null, CancellationToken cancellationToken = default);
}
