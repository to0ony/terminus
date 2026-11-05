using Terminus.Model.Common;

namespace Terminus.Repository.Common;

/// <summary>Upis uvezenog GTFS feeda.</summary>
public interface IGtfsImportRepository
{
    /// <summary>
    /// U jednoj transakciji briše sve podatke voznog reda (i sezone) i upisuje
    /// feed. Korisnici i postavke instance ostaju; ako upis ne uspije, ne mijenja
    /// se ništa.
    /// </summary>
    /// <param name="variants">
    /// Varijante za upis; RouteId, ShapeId i stanice pokazuju na Id-eve iz
    /// <paramref name="feed"/>, a Trip.VariantId na varijante.
    /// </param>
    Task ReplaceAllAsync(GtfsFeedData feed, IReadOnlyList<IRouteVariantModel> variants, CancellationToken cancellationToken = default);
}
