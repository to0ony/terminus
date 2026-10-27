using Microsoft.EntityFrameworkCore;
using Npgsql;
using Terminus.Common;
using Terminus.Common.Exceptions;
using Terminus.DAL;
using Terminus.DAL.Entities;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
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
        var query = FilteredQuery(context, filter);
        var totalCount = await query.CountAsync(cancellationToken);

        var entities = await SortedQuery(context, query, filter)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<IStopModel>(
            entities.Select(StopMapper.ToModel).ToList(),
            filter.Page,
            filter.PageSize,
            totalCount);
    }

    public async Task<PagedResult<StopTableRow>> FindTableAsync(StopFilter filter, CancellationToken cancellationToken = default)
    {
        var page = await FindAsync(filter, cancellationToken);
        var ids = page.Items.Select(s => s.Id).ToList();
        if (ids.Count == 0)
        {
            return page.Map(s => new StopTableRow(s, null, 0, 0, []));
        }

        var parentIds = page.Items.Where(s => s.ParentStationId is not null).Select(s => s.ParentStationId!.Value).ToHashSet();
        var parentNames = await context.Stops
            .AsNoTracking()
            .Where(s => parentIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        // Vožnje staju na peronima, pa kolodvor zbraja svoje perone.
        var platforms = await context.Stops
            .AsNoTracking()
            .Where(s => s.ParentStationId != null && ids.Contains(s.ParentStationId.Value))
            .Select(s => new { s.Id, StationId = s.ParentStationId!.Value })
            .ToListAsync(cancellationToken);
        var stopIds = ids.Concat(platforms.Select(p => p.Id)).ToHashSet();

        var tripCounts = await TripCountsQuery(context, stopIds)
            .ToDictionaryAsync(x => x.StopId, x => x.Trips, cancellationToken);
        var routePairs = await RoutesAtQuery(context, stopIds).ToListAsync(cancellationToken);

        var routeIds = routePairs.Select(p => p.RouteId).ToHashSet();
        var routes = await context.Routes
            .AsNoTracking()
            .Where(r => routeIds.Contains(r.Id))
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.ShortName!.Length)
            .ThenBy(r => r.ShortName)
            .ThenBy(r => r.LongName)
            .ThenBy(r => r.Code)
            .Select(r => new RouteTag(r.Id, r.Code, r.ShortName, r.Color, r.TextColor))
            .ToListAsync(cancellationToken);

        var platformsOf = platforms.ToLookup(p => p.StationId, p => p.Id);
        var routesAt = routePairs.ToLookup(p => p.StopId, p => p.RouteId);

        return page.Map(stop =>
        {
            IReadOnlyList<Guid> served = [stop.Id, .. platformsOf[stop.Id]];
            var routeSet = served.SelectMany(id => routesAt[id]).ToHashSet();
            return new StopTableRow(
                stop,
                stop.ParentStationId is { } parentId ? parentNames.GetValueOrDefault(parentId) : null,
                platformsOf[stop.Id].Count(),
                served.Sum(id => tripCounts.GetValueOrDefault(id)),
                routes.Where(r => routeSet.Contains(r.Id)).ToList());
        });
    }

    /// <summary>Broj različitih vožnji po stanici.</summary>
    internal static IQueryable<StopTripCount> TripCountsQuery(TerminusDbContext context, IReadOnlyCollection<Guid> stopIds) =>
        context.StopTimes
            .AsNoTracking()
            .Where(st => stopIds.Contains(st.StopId))
            .GroupBy(st => st.StopId)
            .Select(g => new StopTripCount { StopId = g.Key, Trips = g.Select(st => st.TripId).Distinct().Count() });

    /// <summary>Parovi stanica – linija za vožnje koje staju na stanicama.</summary>
    internal static IQueryable<StopRoute> RoutesAtQuery(TerminusDbContext context, IReadOnlyCollection<Guid> stopIds) =>
        context.StopTimes
            .AsNoTracking()
            .Where(st => stopIds.Contains(st.StopId))
            .Select(st => new StopRoute { StopId = st.StopId, RouteId = st.Trip.RouteId })
            .Distinct();

    /// <summary>Stanice prema filtru, bez redoslijeda i stranica.</summary>
    internal static IQueryable<Stop> FilteredQuery(TerminusDbContext context, StopFilter filter)
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

        if (filter.WheelchairBoarding is { } wheelchair)
        {
            var value = (short)wheelchair;
            query = query.Where(s => s.WheelchairBoarding == value);
        }

        if (filter.HasCoordinates is bool hasCoordinates)
        {
            query = query.Where(s => (s.Latitude != null && s.Longitude != null) == hasCoordinates);
        }

        // Kolodvor ima vožnje na peronima.
        if (filter.HasTrips is bool hasTrips)
        {
            query = query.Where(s => context.StopTimes
                .Any(st => st.StopId == s.Id || st.Stop.ParentStationId == s.Id) == hasTrips);
        }

        if (filter.RouteId is Guid routeId)
        {
            query = query.Where(s => context.StopTimes
                .Any(st => (st.StopId == s.Id || st.Stop.ParentStationId == s.Id) && st.Trip.RouteId == routeId));
        }

        return query;
    }

    /// <summary>Redoslijed prema filtru; jednaki se slažu po nazivu i kodu.</summary>
    internal static IQueryable<Stop> SortedQuery(TerminusDbContext context, IQueryable<Stop> query, StopFilter filter)
    {
        var descending = filter.SortDescending;
        var sorted = filter.SortBy switch
        {
            StopSortField.Code => query.OrderByDirection(s => s.Code, descending),
            StopSortField.PublicCode => query.OrderByDirection(s => s.PublicCode, descending),
            StopSortField.PlatformCode => query.OrderByDirection(s => s.PlatformCode, descending),
            StopSortField.LocationType => query.OrderByDirection(s => s.LocationType, descending),
            StopSortField.WheelchairBoarding => query.OrderByDirection(s => s.WheelchairBoarding, descending),
            StopSortField.IsActive => query.OrderByDirection(s => s.IsActive, descending),
            StopSortField.UpdatedAt => query.OrderByDirection(s => s.UpdatedAt, descending),
            StopSortField.TripCount => query.OrderByDirection(
                s => context.Trips.Count(t => t.StopTimes.Any(st => st.StopId == s.Id || st.Stop.ParentStationId == s.Id)),
                descending),
            StopSortField.RouteCount => query.OrderByDirection(
                s => context.Routes.Count(r => r.Trips.Any(t => t.StopTimes.Any(st => st.StopId == s.Id || st.Stop.ParentStationId == s.Id))),
                descending),
            _ => query.OrderByDirection(s => s.Name, descending)
        };

        return sorted
            .ThenBy(s => s.Name)
            .ThenBy(s => s.Code);
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

    public async Task<IReadOnlyList<StopSequenceUse>> GetSequencesThroughAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var variants = await VariantsThroughQuery(context, id).ToListAsync(cancellationToken);
        var trips = await TripsThroughQuery(context, id).ToListAsync(cancellationToken);

        return [.. variants, .. trips];
    }

    /// <summary>Izdvojeno da test može provjeriti prevodi li EF upit u SQL.</summary>
    internal static IQueryable<StopSequenceUse> VariantsThroughQuery(TerminusDbContext context, Guid id) =>
        context.RouteVariants
            .AsNoTracking()
            .Where(v => v.Stops.Any(s => s.StopId == id))
            .OrderBy(v => v.Route.SortOrder)
            .ThenBy(v => v.Route.ShortName)
            .ThenBy(v => v.SortOrder)
            .Select(v => new StopSequenceUse(
                true,
                v.Name,
                v.Route.ShortName ?? v.Route.LongName,
                v.Stops.OrderBy(s => s.Sequence).Select(s => s.StopId).ToList()));

    /// <summary>Izdvojeno da test može provjeriti prevodi li EF upit u SQL.</summary>
    internal static IQueryable<StopSequenceUse> TripsThroughQuery(TerminusDbContext context, Guid id) =>
        context.Trips
            .AsNoTracking()
            .Where(t => t.StopTimes.Any(st => st.StopId == id))
            .OrderBy(t => t.Code)
            .Select(t => new StopSequenceUse(
                false,
                t.Code,
                t.Route.ShortName ?? t.Route.LongName,
                t.StopTimes.OrderBy(st => st.StopSequence).Select(st => st.StopId).ToList()));

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

internal sealed class StopTripCount
{
    public Guid StopId { get; init; }
    public int Trips { get; init; }
}

internal sealed class StopRoute
{
    public Guid StopId { get; init; }
    public Guid RouteId { get; init; }
}
