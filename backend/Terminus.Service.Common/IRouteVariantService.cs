using Terminus.Model.Common;

namespace Terminus.Service.Common;

/// <summary>Varijante linija: redoslijed stanica smjera s vremenima vožnje.</summary>
public interface IRouteVariantService
{
    Task<RouteVariants> ListAsync(Guid routeId, CancellationToken cancellationToken = default);

    Task<RouteVariantDetail> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RouteVariantDetail> CreateAsync(Guid routeId, IRouteVariantModel variant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mijenja varijantu. Stanice varijante koju koriste vožnje ne mogu se
    /// promijeniti (409), jer vožnje imaju svoja vremena po tim stanicama.
    /// </summary>
    Task<RouteVariantDetail> UpdateAsync(Guid id, IRouteVariantModel variant, CancellationToken cancellationToken = default);

    /// <summary>Briše varijantu; 409 ako je koriste vožnje.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Prepoznaje varijante iz vožnji linije (isti smjer i redoslijed stanica)
    /// sa srednjim vremenima vožnje i vožnjama postavlja varijantu. Samo za
    /// liniju koja još nema varijanti (inače 409).
    /// </summary>
    Task<RouteVariants> DeriveAsync(Guid routeId, CancellationToken cancellationToken = default);
}

/// <param name="Stops">Stanice svih varijanti po Id-u.</param>
/// <param name="TripCounts">Broj vožnji po varijanti; bez vožnji nije u rječniku.</param>
public sealed record RouteVariants(
    IReadOnlyList<IRouteVariantModel> Variants,
    IReadOnlyDictionary<Guid, IStopModel> Stops,
    IReadOnlyDictionary<Guid, int> TripCounts);

public sealed record RouteVariantDetail(
    IRouteVariantModel Variant,
    IReadOnlyDictionary<Guid, IStopModel> Stops,
    int TripCount);
