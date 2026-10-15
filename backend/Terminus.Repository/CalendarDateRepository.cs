using Microsoft.EntityFrameworkCore;
using Npgsql;
using Terminus.Common.Exceptions;
using Terminus.DAL;
using Terminus.DAL.Entities;
using Terminus.Model.Common;
using Terminus.Repository.Common;
using Terminus.Repository.Mapping;

namespace Terminus.Repository;

public class CalendarDateRepository(TerminusDbContext context) : ICalendarDateRepository
{
    public async Task<IReadOnlyList<ICalendarDateModel>> GetByCalendarAsync(Guid serviceCalendarId, CancellationToken cancellationToken = default)
    {
        var entities = await context.CalendarDates
            .AsNoTracking()
            .Where(d => d.ServiceCalendarId == serviceCalendarId)
            .OrderBy(d => d.Date)
            .ToListAsync(cancellationToken);

        return entities.Select(ServiceCalendarMapper.ToModel).ToList();
    }

    public async Task<ICalendarDateModel> UpsertAsync(ICalendarDateModel calendarDate, CancellationToken cancellationToken = default)
    {
        var entity = await context.CalendarDates.FindAsync(
            [calendarDate.ServiceCalendarId, calendarDate.Date], cancellationToken);

        if (entity is null)
        {
            entity = new CalendarDate
            {
                ServiceCalendarId = calendarDate.ServiceCalendarId,
                Date = calendarDate.Date
            };
            context.CalendarDates.Add(entity);
        }

        entity.ExceptionType = (short)calendarDate.ExceptionType;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is PostgresException { ConstraintName: "pk_calendar_dates" })
        {
            // Netko je istovremeno dodao iznimku za isti datum.
            throw new ConflictException($"Iznimka za {calendarDate.Date:yyyy-MM-dd} je upravo dodana. Pokušaj ponovno.", ex);
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is PostgresException { ConstraintName: "fk_calendar_dates_service_calendars_service_calendar_id" })
        {
            throw new NotFoundException($"Kalendar {calendarDate.ServiceCalendarId} ne postoji.", ex);
        }

        return ServiceCalendarMapper.ToModel(entity);
    }

    public async Task<bool> DeleteAsync(Guid serviceCalendarId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var deleted = await context.CalendarDates
            .Where(d => d.ServiceCalendarId == serviceCalendarId && d.Date == date)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0;
    }
}
