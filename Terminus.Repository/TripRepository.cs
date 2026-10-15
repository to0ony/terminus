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

public class TripRepository(TerminusDbContext context) : ITripRepository
{
    public async Task<ITripModel?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.Trips
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        return entity is null ? null : TripMapper.ToModel(entity);
    }

    public async Task<PagedResult<ITripModel>> FindAsync(TripFilter filter, CancellationToken cancellationToken = default)
    {
        var query = context.Trips
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = LikePattern.Contains(filter.Search);
            query = query.Where(t =>
                EF.Functions.ILike(t.Code, pattern, LikePattern.EscapeCharacter) ||
                EF.Functions.ILike(t.Headsign!, pattern, LikePattern.EscapeCharacter));
        }

        if (filter.RouteId is Guid routeId)
        {
            query = query.Where(t => t.RouteId == routeId);
        }

        if (filter.ServiceCalendarId is Guid serviceCalendarId)
        {
            query = query.Where(t => t.ServiceCalendarId == serviceCalendarId);
        }

        if (filter.DirectionId is { } direction)
        {
            var value = (short)direction;
            query = query.Where(t => t.DirectionId == value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Po prvom polasku; vožnje bez vremena (NULL) idu na kraj.
        var entities = await query
            .OrderBy(t => t.StopTimes.Min(st => (int?)st.DepartureTime))
            .ThenBy(t => t.Code)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ITripModel>(
            entities.Select(TripMapper.ToModel).ToList(),
            filter.Page,
            filter.PageSize,
            totalCount);
    }

    public async Task<ITripModel> InsertAsync(ITripModel trip, CancellationToken cancellationToken = default)
    {
        var entity = new Trip();
        TripMapper.Apply(trip, entity);

        context.Trips.Add(entity);
        await SaveChangesAsync(trip, cancellationToken);

        // Id, CreatedAt i UpdatedAt je vratila baza (RETURNING).
        return TripMapper.ToModel(entity);
    }

    public async Task<ITripModel?> UpdateAsync(ITripModel trip, CancellationToken cancellationToken = default)
    {
        var entity = await context.Trips
            .FirstOrDefaultAsync(t => t.Id == trip.Id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        TripMapper.Apply(trip, entity);
        await SaveChangesAsync(trip, cancellationToken);

        return TripMapper.ToModel(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Vremena (stop_times) se brišu kaskadno.
        var deleted = await context.Trips
            .Where(t => t.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0;
    }

    private async Task SaveChangesAsync(ITripModel trip, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is PostgresException { ConstraintName: "uq_trips_code" })
        {
            throw new ConflictException($"Vožnja s kodom '{trip.Code}' već postoji.", ex);
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is PostgresException { ConstraintName: "fk_trips_routes_route_id" })
        {
            throw new NotFoundException($"Linija {trip.RouteId} ne postoji.", ex);
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is PostgresException { ConstraintName: "fk_trips_service_calendars_service_calendar_id" })
        {
            throw new NotFoundException($"Kalendar {trip.ServiceCalendarId} ne postoji.", ex);
        }
    }
}
