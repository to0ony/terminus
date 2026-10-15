using Microsoft.EntityFrameworkCore;
using Npgsql;
using Terminus.Common;
using Terminus.Common.Exceptions;
using Terminus.DAL;
using Terminus.DAL.Entities;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;
using Terminus.Repository.Common;
using Terminus.Repository.Mapping;

namespace Terminus.Repository;

public class ServiceCalendarRepository(TerminusDbContext context) : IServiceCalendarRepository
{
    public async Task<IServiceCalendarModel?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.ServiceCalendars
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return entity is null ? null : ServiceCalendarMapper.ToModel(entity);
    }

    public async Task<PagedResult<IServiceCalendarModel>> FindAsync(ServiceCalendarFilter filter, CancellationToken cancellationToken = default)
    {
        var query = context.ServiceCalendars
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = LikePattern.Contains(filter.Search);
            query = query.Where(c =>
                EF.Functions.ILike(c.Code, pattern, LikePattern.EscapeCharacter) ||
                EF.Functions.ILike(c.Name, pattern, LikePattern.EscapeCharacter));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var entities = await query
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Code)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<IServiceCalendarModel>(
            entities.Select(ServiceCalendarMapper.ToModel).ToList(),
            filter.Page,
            filter.PageSize,
            totalCount);
    }

    public async Task<IServiceCalendarModel> InsertAsync(IServiceCalendarModel calendar, CancellationToken cancellationToken = default)
    {
        var entity = new ServiceCalendar();
        ServiceCalendarMapper.Apply(calendar, entity);

        context.ServiceCalendars.Add(entity);
        await SaveChangesAsync(calendar, cancellationToken);

        // Id, CreatedAt i UpdatedAt je vratila baza (RETURNING).
        return ServiceCalendarMapper.ToModel(entity);
    }

    public async Task<IServiceCalendarModel?> UpdateAsync(IServiceCalendarModel calendar, CancellationToken cancellationToken = default)
    {
        var entity = await context.ServiceCalendars
            .FirstOrDefaultAsync(c => c.Id == calendar.Id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        ServiceCalendarMapper.Apply(calendar, entity);
        await SaveChangesAsync(calendar, cancellationToken);

        return ServiceCalendarMapper.ToModel(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Iznimke (calendar_dates) se brišu kaskadno.
            var deleted = await context.ServiceCalendars
                .Where(c => c.Id == id)
                .ExecuteDeleteAsync(cancellationToken);

            return deleted > 0;
        }
        catch (Exception ex) when (ex.GetBaseException() is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation,
            ConstraintName: "fk_trips_service_calendars_service_calendar_id"
        })
        {
            throw new ConflictException("Kalendar koriste vožnje i ne može se obrisati.", ex);
        }
    }

    private async Task SaveChangesAsync(IServiceCalendarModel calendar, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is PostgresException { ConstraintName: "uq_service_calendars_code" })
        {
            throw new ConflictException($"Kalendar s kodom '{calendar.Code}' već postoji.", ex);
        }
    }
}
