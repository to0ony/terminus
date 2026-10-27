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

public class RouteRepository(TerminusDbContext context) : IRouteRepository
{
    public async Task<IRouteModel?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.Routes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        return entity is null ? null : RouteMapper.ToModel(entity);
    }

    public async Task<PagedResult<IRouteModel>> FindAsync(RouteFilter filter, CancellationToken cancellationToken = default)
    {
        var query = FilteredQuery(context, filter);
        var totalCount = await query.CountAsync(cancellationToken);

        var entities = await SortedQuery(context, query, filter)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<IRouteModel>(
            entities.Select(RouteMapper.ToModel).ToList(),
            filter.Page,
            filter.PageSize,
            totalCount);
    }

    public async Task<PagedResult<RouteTableRow>> FindTableAsync(RouteFilter filter, CancellationToken cancellationToken = default)
    {
        var page = await FindAsync(filter, cancellationToken);
        var ids = page.Items.Select(r => r.Id).ToList();
        var agencyIds = page.Items.Select(r => r.AgencyId).ToHashSet();

        var agencies = await context.Agencies
            .AsNoTracking()
            .Where(a => agencyIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Name, cancellationToken);

        var variantCounts = await context.RouteVariants
            .AsNoTracking()
            .Where(v => ids.Contains(v.RouteId))
            .GroupBy(v => v.RouteId)
            .Select(g => new { RouteId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RouteId, x => x.Count, cancellationToken);

        var tripCounts = await context.Trips
            .AsNoTracking()
            .Where(t => ids.Contains(t.RouteId))
            .GroupBy(t => t.RouteId)
            .Select(g => new { RouteId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RouteId, x => x.Count, cancellationToken);

        var stopStats = await StopStatsQuery(context, ids)
            .ToDictionaryAsync(x => x.RouteId, cancellationToken);

        return page.Map(route =>
        {
            var stats = stopStats.GetValueOrDefault(route.Id);
            return new RouteTableRow(
                route,
                agencies.GetValueOrDefault(route.AgencyId) ?? "",
                variantCounts.GetValueOrDefault(route.Id),
                tripCounts.GetValueOrDefault(route.Id),
                stats?.StopCount ?? 0,
                stats?.FirstDeparture,
                stats?.LastArrival);
        });
    }

    /// <summary>Po liniji: različite stanice, najraniji polazak i najkasniji dolazak njezinih vožnji.</summary>
    internal static IQueryable<RouteStopStats> StopStatsQuery(TerminusDbContext context, IReadOnlyCollection<Guid> routeIds) =>
        context.StopTimes
            .AsNoTracking()
            .Where(st => routeIds.Contains(st.Trip.RouteId))
            .GroupBy(st => st.Trip.RouteId)
            .Select(g => new RouteStopStats
            {
                RouteId = g.Key,
                StopCount = g.Select(st => st.StopId).Distinct().Count(),
                FirstDeparture = g.Min(st => st.DepartureTime),
                LastArrival = g.Max(st => st.ArrivalTime)
            });

    /// <summary>Linije prema filtru, bez redoslijeda i stranica.</summary>
    internal static IQueryable<Route> FilteredQuery(TerminusDbContext context, RouteFilter filter)
    {
        var query = context.Routes
            .AsNoTracking();

        if (filter.AgencyId is Guid agencyId)
        {
            query = query.Where(r => r.AgencyId == agencyId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = LikePattern.Contains(filter.Search);
            query = query.Where(r =>
                EF.Functions.ILike(r.Code, pattern, LikePattern.EscapeCharacter) ||
                EF.Functions.ILike(r.ShortName!, pattern, LikePattern.EscapeCharacter) ||
                EF.Functions.ILike(r.LongName!, pattern, LikePattern.EscapeCharacter));
        }

        if (filter.IsActive is bool isActive)
        {
            query = query.Where(r => r.IsActive == isActive);
        }

        if (filter.RouteType is { } routeType)
        {
            var value = (short)routeType;
            query = query.Where(r => r.RouteType == value);
        }

        if (filter.HasTrips is bool hasTrips)
        {
            query = query.Where(r => r.Trips.Any() == hasTrips);
        }

        if (filter.StopId is Guid stopId)
        {
            query = query.Where(r => r.Trips.Any(t => t.StopTimes
                .Any(st => st.StopId == stopId || st.Stop.ParentStationId == stopId)));
        }

        return query;
    }

    /// <summary>
    /// Redoslijed prema filtru; jednaki (i zadani redoslijed) kao u GTFS-u:
    /// route_sort_order (NULL na kraju), zatim oznaka (kraća prije duže, pa 2
    /// dolazi prije 11 i 101) i naziv.
    /// </summary>
    internal static IQueryable<Route> SortedQuery(TerminusDbContext context, IQueryable<Route> query, RouteFilter filter)
    {
        var descending = filter.SortDescending;
        var sorted = filter.SortBy switch
        {
            RouteSortField.Code => query.OrderByDirection(r => r.Code, descending),
            RouteSortField.ShortName => query
                .OrderByDirection(r => r.ShortName!.Length, descending)
                .ThenByDirection(r => r.ShortName, descending),
            RouteSortField.LongName => query.OrderByDirection(r => r.LongName, descending),
            RouteSortField.RouteType => query.OrderByDirection(r => r.RouteType, descending),
            RouteSortField.Agency => query.OrderByDirection(r => r.Agency.Name, descending),
            RouteSortField.IsActive => query.OrderByDirection(r => r.IsActive, descending),
            RouteSortField.VariantCount => query.OrderByDirection(
                r => context.RouteVariants.Count(v => v.RouteId == r.Id), descending),
            RouteSortField.TripCount => query.OrderByDirection(r => r.Trips.Count, descending),
            RouteSortField.UpdatedAt => query.OrderByDirection(r => r.UpdatedAt, descending),
            _ => query.OrderByDirection(r => r.SortOrder, descending)
        };

        return sorted
            .ThenBy(r => r.SortOrder)
            .ThenBy(r => r.ShortName!.Length)
            .ThenBy(r => r.ShortName)
            .ThenBy(r => r.LongName)
            .ThenBy(r => r.Code);
    }

    public async Task<IRouteModel> InsertAsync(IRouteModel route, CancellationToken cancellationToken = default)
    {
        var entity = new Route();
        RouteMapper.Apply(route, entity);

        context.Routes.Add(entity);
        await SaveChangesAsync(route, cancellationToken);

        // Id, CreatedAt i UpdatedAt je vratila baza (RETURNING).
        return RouteMapper.ToModel(entity);
    }

    public async Task<IRouteModel?> UpdateAsync(IRouteModel route, CancellationToken cancellationToken = default)
    {
        var entity = await context.Routes
            .FirstOrDefaultAsync(r => r.Id == route.Id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var modeChanged = entity.TimeMode != (short)route.TimeMode;
        RouteMapper.Apply(route, entity);

        if (!modeChanged)
        {
            await SaveChangesAsync(route, cancellationToken);
            return RouteMapper.ToModel(entity);
        }

        // ExecuteUpdate se izvršava odmah, izvan SaveChanges, pa ide u istu transakciju.
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await SaveChangesAsync(route, cancellationToken);

        // Promjena načina mijenja samo oznaku vremena međustanica (GTFS timepoint);
        // vremena ostaju ista. Prva i zadnja stanica su uvijek točne.
        var exact = route.TimeMode == TimeMode.Exact;
        await context.StopTimes
            .Where(st => st.Trip.RouteId == route.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(
                st => st.Timepoint,
                st => exact
                    || st.StopSequence == st.Trip.StopTimes.Min(x => x.StopSequence)
                    || st.StopSequence == st.Trip.StopTimes.Max(x => x.StopSequence)),
                cancellationToken);

        // Promjena vremena je promjena vožnje (bitno npr. za GTFS izvoz i otvoreni grafikon).
        await context.Trips
            .Where(t => t.RouteId == route.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UpdatedAt, DateTime.UtcNow), cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return RouteMapper.ToModel(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // ON DELETE CASCADE u bazi briše i sve vožnje linije (i njihova vremena).
        var deleted = await context.Routes
            .Where(r => r.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0;
    }

    private async Task SaveChangesAsync(IRouteModel route, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is PostgresException { ConstraintName: "uq_routes_code" })
        {
            throw new ConflictException($"Linija s kodom '{route.Code}' već postoji.", ex);
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is PostgresException { ConstraintName: "fk_routes_agencies_agency_id" })
        {
            // Servis provjerava operatera; ovo pokriva brisanje operatera u međuvremenu.
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(route.AgencyId)] = ["Prijevoznik ne postoji."]
            });
        }
    }
}

internal sealed class RouteStopStats
{
    public Guid RouteId { get; init; }
    public int StopCount { get; init; }
    public int? FirstDeparture { get; init; }
    public int? LastArrival { get; init; }
}
