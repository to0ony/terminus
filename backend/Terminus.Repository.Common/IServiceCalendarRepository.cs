using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;

namespace Terminus.Repository.Common;

public interface IServiceCalendarRepository
{
    Task<IServiceCalendarModel?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<IServiceCalendarModel>> FindAsync(ServiceCalendarFilter filter, CancellationToken cancellationToken = default);

    Task<IServiceCalendarModel> InsertAsync(IServiceCalendarModel calendar, CancellationToken cancellationToken = default);

    /// <returns>Ažurirani kalendar ili null ako ne postoji.</returns>
    Task<IServiceCalendarModel?> UpdateAsync(IServiceCalendarModel calendar, CancellationToken cancellationToken = default);

    /// <returns>false ako kalendar ne postoji.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
