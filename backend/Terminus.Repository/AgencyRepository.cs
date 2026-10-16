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

public class AgencyRepository(TerminusDbContext context) : IAgencyRepository
{
    public async Task<IAgencyModel?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.Agencies
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        return entity is null ? null : AgencyMapper.ToModel(entity);
    }

    public async Task<PagedResult<IAgencyModel>> FindAsync(AgencyFilter filter, CancellationToken cancellationToken = default)
    {
        var query = context.Agencies.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = LikePattern.Contains(filter.Search);
            query = query.Where(a => EF.Functions.ILike(a.Name, pattern, LikePattern.EscapeCharacter));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var entities = await query
            .OrderBy(a => a.Name)
            .ThenBy(a => a.Id)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<IAgencyModel>(
            entities.Select(AgencyMapper.ToModel).ToList(),
            filter.Page,
            filter.PageSize,
            totalCount);
    }

    public async Task<IAgencyModel> InsertAsync(IAgencyModel agency, CancellationToken cancellationToken = default)
    {
        var entity = new Agency();
        AgencyMapper.Apply(agency, entity);

        context.Agencies.Add(entity);
        await SaveChangesAsync(agency, cancellationToken);

        // Id, CreatedAt i UpdatedAt je vratila baza (RETURNING).
        return AgencyMapper.ToModel(entity);
    }

    public async Task<IAgencyModel?> UpdateAsync(IAgencyModel agency, CancellationToken cancellationToken = default)
    {
        var entity = await context.Agencies.FirstOrDefaultAsync(a => a.Id == agency.Id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        AgencyMapper.Apply(agency, entity);
        await SaveChangesAsync(agency, cancellationToken);

        return AgencyMapper.ToModel(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var deleted = await context.Agencies
                .Where(a => a.Id == id)
                .ExecuteDeleteAsync(cancellationToken);

            return deleted > 0;
        }
        catch (Exception ex) when (ex.GetBaseException() is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation,
            ConstraintName: "fk_routes_agencies_agency_id"
        })
        {
            throw new ConflictException("Prijevoznik ima linije i ne može se obrisati.", ex);
        }
        catch (Exception ex) when (ex.GetBaseException() is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation,
            ConstraintName: "fk_fares_agencies_agency_id"
        })
        {
            throw new ConflictException("Prijevoznik ima cijene karata i ne može se obrisati.", ex);
        }
    }

    public Task<string?> GetOtherTimezoneAsync(Guid exceptId, string timezone, CancellationToken cancellationToken = default) =>
        context.Agencies
            .AsNoTracking()
            .Where(a => a.Id != exceptId && a.Timezone != timezone)
            .Select(a => a.Timezone)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default) =>
        context.Agencies.AnyAsync(cancellationToken);

    private async Task SaveChangesAsync(IAgencyModel agency, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is PostgresException { ConstraintName: "uq_agencies_code" })
        {
            throw new ConflictException($"Prijevoznik s kodom '{agency.Code}' već postoji.", ex);
        }
    }
}
