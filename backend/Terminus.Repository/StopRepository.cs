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

public class StopRepository(TerminusDbContext context) : IStopRepository
{
    public async Task<IStopModel?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.Stops
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        return entity is null ? null : StopMapper.ToModel(entity);
    }

    public async Task<IStopModel?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var entity = await context.Stops
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Code == code, cancellationToken);

        return entity is null ? null : StopMapper.ToModel(entity);
    }

    public async Task<IReadOnlyList<IStopModel>> GetManyAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        var entities = await context.Stops
            .AsNoTracking()
            .Where(s => ids.Contains(s.Id))
            .ToListAsync(cancellationToken);

        return entities.Select(StopMapper.ToModel).ToList();
    }

    public async Task<PagedResult<IStopModel>> FindAsync(StopFilter filter, CancellationToken cancellationToken = default)
    {
        var query = context.Stops
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = LikePattern.Contains(filter.Search);
            query = query.Where(s =>
                EF.Functions.ILike(s.Name, pattern, LikePattern.EscapeCharacter) ||
                EF.Functions.ILike(s.Code, pattern, LikePattern.EscapeCharacter));
        }

        if (filter.IsActive is bool isActive)
        {
            query = query.Where(s => s.IsActive == isActive);
        }

        if (filter.LocationType is { } locationType)
        {
            var value = (short)locationType;
            query = query.Where(s => s.LocationType == value);
        }

        if (filter.ParentStationId is Guid parentStationId)
        {
            query = query.Where(s => s.ParentStationId == parentStationId);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var entities = await query
            .OrderBy(s => s.Name)
            .ThenBy(s => s.Code)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<IStopModel>(
            entities.Select(StopMapper.ToModel).ToList(),
            filter.Page,
            filter.PageSize,
            totalCount);
    }

    public async Task<IReadOnlyList<IStopModel>> FindInAreaAsync(GeoBounds bounds, bool? isActive, int limit, CancellationToken cancellationToken = default)
    {
        var query = context.Stops
            .AsNoTracking()
            .Where(s => s.Latitude >= bounds.MinLatitude
                && s.Latitude <= bounds.MaxLatitude
                && s.Longitude >= bounds.MinLongitude
                && s.Longitude <= bounds.MaxLongitude);

        if (isActive is bool active)
        {
            query = query.Where(s => s.IsActive == active);
        }

        var entities = await query
            .OrderBy(s => s.Name)
            .ThenBy(s => s.Code)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return entities.Select(StopMapper.ToModel).ToList();
    }

    public async Task<GeoBounds?> GetExtentAsync(CancellationToken cancellationToken = default)
    {
        // Feedovi znaju imati pokoju stanicu s krivim koordinatama (npr. tisuće
        // km od mreže). Zato se gledaju samo stanice unutar 1. i 99. percentila,
        // proširenih za pola tog raspona na svaku stranu; rubna naselja ostaju.
        var rows = await context.Database
            .SqlQueryRaw<ExtentRow>("""
                WITH p AS (
                    SELECT percentile_cont(0.01) WITHIN GROUP (ORDER BY latitude) AS lat_lo,
                           percentile_cont(0.99) WITHIN GROUP (ORDER BY latitude) AS lat_hi,
                           percentile_cont(0.01) WITHIN GROUP (ORDER BY longitude) AS lon_lo,
                           percentile_cont(0.99) WITHIN GROUP (ORDER BY longitude) AS lon_hi
                    FROM stops
                    WHERE latitude IS NOT NULL AND longitude IS NOT NULL
                )
                SELECT min(s.latitude) AS min_latitude, min(s.longitude) AS min_longitude,
                       max(s.latitude) AS max_latitude, max(s.longitude) AS max_longitude
                FROM stops s, p
                WHERE s.latitude BETWEEN p.lat_lo - (p.lat_hi - p.lat_lo) / 2 AND p.lat_hi + (p.lat_hi - p.lat_lo) / 2
                  AND s.longitude BETWEEN p.lon_lo - (p.lon_hi - p.lon_lo) / 2 AND p.lon_hi + (p.lon_hi - p.lon_lo) / 2
                """)
            .ToListAsync(cancellationToken);

        return rows.SingleOrDefault() is { MinLatitude: double minLat, MinLongitude: double minLon, MaxLatitude: double maxLat, MaxLongitude: double maxLon }
            ? new GeoBounds(minLat, minLon, maxLat, maxLon)
            : null;
    }

    private sealed record ExtentRow(double? MinLatitude, double? MinLongitude, double? MaxLatitude, double? MaxLongitude);

    public Task<bool> HasChildStopsAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Stops.AnyAsync(s => s.ParentStationId == id, cancellationToken);

    public Task<bool> IsUsedByTripsAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.StopTimes.AnyAsync(st => st.StopId == id, cancellationToken);

    public async Task<IStopModel> InsertAsync(IStopModel stop, CancellationToken cancellationToken = default)
    {
        var entity = new Stop();
        StopMapper.Apply(stop, entity);

        context.Stops.Add(entity);
        await SaveChangesAsync(stop, cancellationToken);

        // Id, CreatedAt i UpdatedAt je vratila baza (RETURNING).
        return StopMapper.ToModel(entity);
    }

    public async Task<IStopModel?> UpdateAsync(IStopModel stop, CancellationToken cancellationToken = default)
    {
        var entity = await context.Stops
            .FirstOrDefaultAsync(s => s.Id == stop.Id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        StopMapper.Apply(stop, entity);
        await SaveChangesAsync(stop, cancellationToken);

        return StopMapper.ToModel(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Peroni ove stanice dobivaju parent_station_id = NULL (ON DELETE SET NULL).
            var deleted = await context.Stops
                .Where(s => s.Id == id)
                .ExecuteDeleteAsync(cancellationToken);

            return deleted > 0;
        }
        catch (Exception ex) when (ex.GetBaseException() is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation,
            ConstraintName: "fk_stop_times_stops_stop_id"
        })
        {
            throw new ConflictException("Stanica se koristi u voznom redu i ne može se obrisati.", ex);
        }
        catch (Exception ex) when (ex.GetBaseException() is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation,
            ConstraintName: "fk_route_variant_stops_stops_stop_id"
        })
        {
            throw new ConflictException("Stanica je u varijanti linije i ne može se obrisati.", ex);
        }
    }

    private async Task SaveChangesAsync(IStopModel stop, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is PostgresException { ConstraintName: "uq_stops_code" })
        {
            throw new ConflictException($"Stanica s kodom '{stop.Code}' već postoji.", ex);
        }
    }
}
