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
