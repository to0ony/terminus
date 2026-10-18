using Terminus.Model.Common;

namespace Terminus.Repository.Common;

/// <summary>Varijante linija sa stanicama.</summary>
public interface IRouteVariantRepository
{
    /// <returns>Varijante linije po smjeru i redoslijedu prikaza.</returns>
    Task<IReadOnlyList<IRouteVariantModel>> ListByRouteAsync(Guid routeId, CancellationToken cancellationToken = default);

    Task<IRouteVariantModel?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <returns>Broj vožnji po varijanti; varijante bez vožnji nisu u rječniku.</returns>
    Task<IReadOnlyDictionary<Guid, int>> CountTripsAsync(IReadOnlyCollection<Guid> variantIds, CancellationToken cancellationToken = default);

    Task<IRouteVariantModel> InsertAsync(IRouteVariantModel variant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mijenja varijantu i zamjenjuje joj stanice. Nova trasa prelazi i na vožnje
    /// varijante, a stara se briše ako je više ništa ne koristi.
    /// </summary>
    /// <returns>Izmijenjena varijanta ili null ako ne postoji.</returns>
    Task<IRouteVariantModel?> UpdateAsync(IRouteVariantModel variant, CancellationToken cancellationToken = default);

    /// <returns>false ako varijanta ne postoji.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dodaje varijante (Id-eve zadaje pozivatelj) i vožnjama postavlja
    /// varijantu, u jednoj transakciji.
    /// </summary>
    /// <param name="tripVariants">Vožnja → varijanta.</param>
    Task InsertManyAsync(
        IReadOnlyList<IRouteVariantModel> variants,
        IReadOnlyDictionary<Guid, Guid> tripVariants,
        CancellationToken cancellationToken = default);
}
