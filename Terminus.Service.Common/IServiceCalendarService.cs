using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Model.Common.Filters;

namespace Terminus.Service.Common;

/// <summary>Kalendari (GTFS calendar) i njihove iznimke (GTFS calendar_dates).</summary>
public interface IServiceCalendarService
{
    Task<IServiceCalendarModel> GetAsync(Guid agencyId, Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<IServiceCalendarModel>> FindAsync(Guid agencyId, ServiceCalendarFilter filter, CancellationToken cancellationToken = default);

    Task<IServiceCalendarModel> CreateAsync(Guid agencyId, IServiceCalendarModel calendar, CancellationToken cancellationToken = default);

    Task<IServiceCalendarModel> UpdateAsync(Guid agencyId, Guid id, IServiceCalendarModel calendar, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid agencyId, Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ICalendarDateModel>> GetDatesAsync(Guid agencyId, Guid calendarId, CancellationToken cancellationToken = default);

    /// <summary>Dodaje iznimku za datum ili mijenja postojeću.</summary>
    Task<ICalendarDateModel> SetDateAsync(Guid agencyId, Guid calendarId, DateOnly date, CalendarExceptionType exceptionType, CancellationToken cancellationToken = default);

    Task RemoveDateAsync(Guid agencyId, Guid calendarId, DateOnly date, CancellationToken cancellationToken = default);
}
