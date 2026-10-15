using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;

namespace Terminus.Repository.Common;

public interface ITripRepository
{
    Task<ITripModel?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <remarks>Poredano po prvom polasku vožnje, zatim po kodu.</remarks>
    Task<PagedResult<ITripModel>> FindAsync(TripFilter filter, CancellationToken cancellationToken = default);

    Task<ITripModel> InsertAsync(ITripModel trip, CancellationToken cancellationToken = default);

    /// <returns>Ažurirana vožnja ili null ako ne postoji.</returns>
    Task<ITripModel?> UpdateAsync(ITripModel trip, CancellationToken cancellationToken = default);

    /// <returns>false ako vožnja ne postoji.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
