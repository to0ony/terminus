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

public class FareRepository(TerminusDbContext context) : IFareRepository
{
    public async Task<IFareModel?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.Fares
            .AsNoTracking()
            .Include(f => f.Rules)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        return entity is null ? null : FareMapper.ToModel(entity, entity.Rules);
    }

    public async Task<PagedResult<IFareModel>> FindAsync(FareFilter filter, CancellationToken cancellationToken = default)
    {
        var query = context.Fares.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = LikePattern.Contains(filter.Search);
            query = query.Where(f => EF.Functions.ILike(f.Code, pattern, LikePattern.EscapeCharacter));
        }

        if (filter.AgencyId is Guid agencyId)
        {
            query = query.Where(f => f.AgencyId == agencyId);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var entities = await query
            .OrderBy(f => f.Code)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Include(f => f.Rules)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return new PagedResult<IFareModel>(
            entities.Select(f => FareMapper.ToModel(f, f.Rules)).ToList(),
            filter.Page,
            filter.PageSize,
            totalCount);
    }

    public async Task<IFareModel> InsertAsync(IFareModel fare, CancellationToken cancellationToken = default)
    {
        var entity = new Fare();
        FareMapper.Apply(fare, entity);

        for (var i = 0; i < fare.Rules.Count; i++)
        {
            entity.Rules.Add(FareMapper.ToEntity(Guid.Empty, i + 1, fare.Rules[i]));
        }

        context.Fares.Add(entity);
        await SaveChangesAsync(fare, cancellationToken);

        return FareMapper.ToModel(entity, entity.Rules);
    }

    public async Task<IFareModel?> UpdateAsync(IFareModel fare, CancellationToken cancellationToken = default)
    {
        var entity = await context.Fares.FirstOrDefaultAsync(f => f.Id == fare.Id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        // ExecuteDelete se izvršava odmah, izvan SaveChanges, pa sve mora biti u istoj transakciji.
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        await context.FareRules
            .Where(r => r.FareId == fare.Id)
            .ExecuteDeleteAsync(cancellationToken);

        FareMapper.Apply(fare, entity);
        context.Entry(entity).Property(e => e.UpdatedAt).IsModified = true;

        var rules = fare.Rules.Select((rule, i) => FareMapper.ToEntity(entity.Id, i + 1, rule)).ToList();
        context.FareRules.AddRange(rules);

        await SaveChangesAsync(fare, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return FareMapper.ToModel(entity, rules);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Pravila se brišu kaskadno.
        var deleted = await context.Fares
            .Where(f => f.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0;
    }

    private async Task SaveChangesAsync(IFareModel fare, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is PostgresException { ConstraintName: "uq_fares_code" })
        {
            throw new ConflictException($"Cijena s kodom '{fare.Code}' već postoji.", ex);
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is PostgresException { ConstraintName: "fk_fares_agencies_agency_id" })
        {
            throw new NotFoundException($"Prijevoznik {fare.AgencyId} ne postoji.", ex);
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is PostgresException { ConstraintName: "fk_fare_rules_routes_route_id" })
        {
            throw new NotFoundException("Jedna od linija u pravilima u međuvremenu je obrisana.", ex);
        }
    }
}
