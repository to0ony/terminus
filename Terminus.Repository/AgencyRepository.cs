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
        await context.SaveChangesAsync(cancellationToken);

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
        await context.SaveChangesAsync(cancellationToken);

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
    }

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default) =>
        context.Agencies.AnyAsync(cancellationToken);
}
