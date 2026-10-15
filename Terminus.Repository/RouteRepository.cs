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

        var totalCount = await query.CountAsync(cancellationToken);

        // Redoslijed kao u GTFS-u: route_sort_order (NULL na kraju), zatim oznaka i naziv.
        var entities = await query
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.ShortName)
            .ThenBy(r => r.LongName)
            .ThenBy(r => r.Code)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<IRouteModel>(
            entities.Select(RouteMapper.ToModel).ToList(),
            filter.Page,
            filter.PageSize,
            totalCount);
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

        RouteMapper.Apply(route, entity);
        await SaveChangesAsync(route, cancellationToken);

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
