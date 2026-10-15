using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Model.Common.Filters;

namespace Terminus.Service.Common;

/// <summary>Kalendari (GTFS calendar) i njihove iznimke (GTFS calendar_dates).</summary>
public interface IServiceCalendarService
{
    Task<IServiceCalendarModel> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<IServiceCalendarModel>> FindAsync(ServiceCalendarFilter filter, CancellationToken cancellationToken = default);

    Task<IServiceCalendarModel> CreateAsync(IServiceCalendarModel calendar, CancellationToken cancellationToken = default);

    Task<IServiceCalendarModel> UpdateAsync(Guid id, IServiceCalendarModel calendar, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ICalendarDateModel>> GetDatesAsync(Guid calendarId, CancellationToken cancellationToken = default);

    /// <summary>Dodaje iznimku za datum ili mijenja postojeću.</summary>
    Task<ICalendarDateModel> SetDateAsync(Guid calendarId, DateOnly date, CalendarExceptionType exceptionType, CancellationToken cancellationToken = default);

    Task RemoveDateAsync(Guid calendarId, DateOnly date, CancellationToken cancellationToken = default);
}
