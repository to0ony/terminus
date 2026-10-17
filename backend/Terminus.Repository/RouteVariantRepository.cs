using Microsoft.EntityFrameworkCore;
using Terminus.DAL;
using Terminus.DAL.Entities;
using Terminus.Model.Common;
using Terminus.Repository.Common;
using Terminus.Repository.Mapping;

namespace Terminus.Repository;

public class RouteVariantRepository(TerminusDbContext context) : IRouteVariantRepository
{
    public async Task<IReadOnlyList<IRouteVariantModel>> ListByRouteAsync(Guid routeId, CancellationToken cancellationToken = default)
    {
        var variants = await context.RouteVariants
            .AsNoTracking()
            .Include(v => v.Stops)
            .Where(v => v.RouteId == routeId)
            .OrderBy(v => v.DirectionId == null)
            .ThenBy(v => v.DirectionId)
            .ThenBy(v => v.SortOrder)
            .ThenBy(v => v.Name)
            .ToListAsync(cancellationToken);

        return variants.Select(RouteVariantMapper.ToModel).ToList();
    }

    public async Task<IRouteVariantModel?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var variant = await context.RouteVariants
            .AsNoTracking()
            .Include(v => v.Stops)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        return variant is null ? null : RouteVariantMapper.ToModel(variant);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> CountTripsAsync(IReadOnlyCollection<Guid> variantIds, CancellationToken cancellationToken = default) =>
        await context.Trips
            .Where(t => t.VariantId != null && variantIds.Contains(t.VariantId.Value))
            .GroupBy(t => t.VariantId!.Value)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

    public async Task<IRouteVariantModel> InsertAsync(IRouteVariantModel variant, CancellationToken cancellationToken = default)
    {
        var entity = new RouteVariant { Id = variant.Id == Guid.Empty ? Guid.NewGuid() : variant.Id };
        RouteVariantMapper.Apply(variant, entity);
        context.RouteVariants.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return RouteVariantMapper.ToModel(entity);
    }

    public async Task<IRouteVariantModel?> UpdateAsync(IRouteVariantModel variant, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var entity = await context.RouteVariants.FirstOrDefaultAsync(v => v.Id == variant.Id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        // Stanice se pišu ispočetka.
        await context.RouteVariantStops.Where(s => s.VariantId == variant.Id).ExecuteDeleteAsync(cancellationToken);
        RouteVariantMapper.Apply(variant, entity);
        context.RouteVariantStops.AddRange(entity.Stops);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetAsync(variant.Id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.RouteVariants.Where(v => v.Id == id).ExecuteDeleteAsync(cancellationToken) > 0;

    public async Task InsertManyAsync(
        IReadOnlyList<IRouteVariantModel> variants,
        IReadOnlyDictionary<Guid, Guid> tripVariants,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        foreach (var variant in variants)
        {
            var entity = new RouteVariant { Id = variant.Id };
            RouteVariantMapper.Apply(variant, entity);
            context.RouteVariants.Add(entity);
        }
        await context.SaveChangesAsync(cancellationToken);

        foreach (var group in tripVariants.GroupBy(kv => kv.Value, kv => kv.Key))
        {
            var tripIds = group.ToList();
            await context.Trips
                .Where(t => tripIds.Contains(t.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.VariantId, group.Key), cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
