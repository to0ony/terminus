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
        var row = await WithSummary(context.Trips.AsNoTracking().Where(t => t.Id == id))
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : ToModel(row);
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
        var page = query
            .OrderBy(t => t.StopTimes.Min(st => (int?)st.DepartureTime))
            .ThenBy(t => t.Code)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize);

        var rows = await WithSummary(page).ToListAsync(cancellationToken);

        return new PagedResult<ITripModel>(
            rows.Select(ToModel).ToList(),
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

        // Id, CreatedAt i UpdatedAt je vratila baza (RETURNING). Nova vožnja još nema stanica.
        var created = TripMapper.ToModel(entity);
        created.Summary = new TripSummary(0, null, null, null, null);
        return created;
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

        // Ponovno čitanje zbog sažetka (vremena se ovdje ne mijenjaju).
        return await GetAsync(entity.Id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Vremena (stop_times) se brišu kaskadno.
        var deleted = await context.Trips
            .Where(t => t.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0;
    }

    /// <summary>Vožnja s brojem stanica te prvom i zadnjom stanicom (za popise u dashboardu).</summary>
    private static IQueryable<TripRow> WithSummary(IQueryable<Trip> trips) =>
        trips.Select(t => new TripRow
        {
            Trip = t,
            StopCount = t.StopTimes.Count,
            First = t.StopTimes
                .OrderBy(st => st.StopSequence)
                .Select(st => new StopTimeEnd { Time = st.DepartureTime, StopName = st.Stop.Name })
                .FirstOrDefault(),
            Last = t.StopTimes
                .OrderByDescending(st => st.StopSequence)
                .Select(st => new StopTimeEnd { Time = st.ArrivalTime, StopName = st.Stop.Name })
                .FirstOrDefault()
        });

    private static ITripModel ToModel(TripRow row)
    {
        var model = TripMapper.ToModel(row.Trip);
        model.Summary = new TripSummary(row.StopCount, row.First?.Time, row.First?.StopName, row.Last?.Time, row.Last?.StopName);
        return model;
    }

    private sealed class TripRow
    {
        public required Trip Trip { get; init; }
        public int StopCount { get; init; }
        public StopTimeEnd? First { get; init; }
        public StopTimeEnd? Last { get; init; }
    }

    private sealed class StopTimeEnd
    {
        public int Time { get; init; }
        public required string StopName { get; init; }
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
