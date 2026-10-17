using Terminus.Model.Common;
using Terminus.Repository.Common;

namespace Terminus.Tests.Fakes;

/// <summary>Varijante u memoriji; vožnje kojima je postavljena varijanta su u <see cref="TripVariants"/>.</summary>
internal sealed class FakeRouteVariantRepository : IRouteVariantRepository
{
    public List<IRouteVariantModel> Variants { get; } = [];
    public Dictionary<Guid, int> TripCounts { get; } = [];
    public Dictionary<Guid, Guid> TripVariants { get; } = [];

    public Task<IReadOnlyList<IRouteVariantModel>> ListByRouteAsync(Guid routeId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IRouteVariantModel>>(Variants
            .Where(v => v.RouteId == routeId)
            .OrderBy(v => v.DirectionId)
            .ThenBy(v => v.SortOrder)
            .ToList());

    public Task<IRouteVariantModel?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Variants.FirstOrDefault(v => v.Id == id));

    public Task<IReadOnlyDictionary<Guid, int>> CountTripsAsync(IReadOnlyCollection<Guid> variantIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, int>>(
            TripCounts.Where(kv => variantIds.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value));

    public Task<IRouteVariantModel> InsertAsync(IRouteVariantModel variant, CancellationToken cancellationToken = default)
    {
        variant.Id = Guid.NewGuid();
        Variants.Add(variant);
        return Task.FromResult(variant);
    }

    public Task<IRouteVariantModel?> UpdateAsync(IRouteVariantModel variant, CancellationToken cancellationToken = default)
    {
        var index = Variants.FindIndex(v => v.Id == variant.Id);
        if (index < 0)
        {
            return Task.FromResult<IRouteVariantModel?>(null);
        }
        Variants[index] = variant;
        return Task.FromResult<IRouteVariantModel?>(variant);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Variants.RemoveAll(v => v.Id == id) > 0);

    public Task InsertManyAsync(
        IReadOnlyList<IRouteVariantModel> variants,
        IReadOnlyDictionary<Guid, Guid> tripVariants,
        CancellationToken cancellationToken = default)
    {
        Variants.AddRange(variants);
        foreach (var (trip, variant) in tripVariants)
        {
            TripVariants[trip] = variant;
        }
        return Task.CompletedTask;
    }
}
