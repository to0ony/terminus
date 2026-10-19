using Terminus.Model.Common;

namespace Terminus.Repository.Common;

/// <summary>Spremanje izmjena voznog reda linije iz grafikona, u jednoj transakciji.</summary>
public interface IScheduleRepository
{
    /// <returns>Postojeće vožnje po Id-u; nepostojeće nisu u rječniku.</returns>
    Task<IReadOnlyDictionary<Guid, ITripModel>> GetTripsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    /// <returns>Sve vožnje zadanih polazaka, s vremenima.</returns>
    Task<IReadOnlyList<TripStopTimes>> GetDepartureTripsAsync(IReadOnlyCollection<Guid> departureIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Briše vožnje <paramref name="deletes"/>, a vožnje iz <paramref name="upserts"/>
    /// dodaje (ako ne postoje) ili mijenja te im zamjenjuje vremena i polazak. Id-eve
    /// novih vožnji zadaje pozivatelj. Polazak s jednom preostalom vožnjom se ukida.
    /// </summary>
    Task SaveAsync(IReadOnlyList<TripStopTimes> upserts, IReadOnlyCollection<Guid> deletes, CancellationToken cancellationToken = default);
}
