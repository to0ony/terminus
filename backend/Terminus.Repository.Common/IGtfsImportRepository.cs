using Terminus.Model.Common;

namespace Terminus.Repository.Common;

/// <summary>Upis osnove iz uvezenog GTFS feeda.</summary>
public interface IGtfsImportRepository
{
    /// <summary>GTFS ID-evi koji već postoje u bazi, da pregled kaže što je novo.</summary>
    Task<GtfsExistingCodes> GetExistingCodesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// U jednoj transakciji dopunjava osnovu: prijevoznike, stanice, linije i
    /// trase s istim GTFS ID-em ažurira, a nove dodaje; dodaje varijante; cijene
    /// i feed_info zamjenjuje. Kalendare, vožnje i sezone iz feeda ne upisuje i
    /// postojeće ne dira.
    /// </summary>
    /// <param name="variants">
    /// Varijante za upis; RouteId, ShapeId i stanice pokazuju na Id-eve iz
    /// <paramref name="feed"/>. Postojeće varijante iz uvoza bez vožnji se
    /// zamjenjuju, a ručne i one s vožnjama ostaju.
    /// </param>
    Task MergeAsync(GtfsFeedData feed, IReadOnlyList<IRouteVariantModel> variants, CancellationToken cancellationToken = default);
}

/// <summary>Postojeći GTFS ID-evi po vrsti zapisa.</summary>
public sealed record GtfsExistingCodes(
    IReadOnlySet<string> Agencies,
    IReadOnlySet<string> Stops,
    IReadOnlySet<string> Routes,
    IReadOnlySet<string> Shapes);
