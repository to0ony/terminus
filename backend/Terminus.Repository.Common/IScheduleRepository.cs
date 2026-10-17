using Terminus.Model.Common;

namespace Terminus.Repository.Common;

/// <summary>Spremanje izmjena voznog reda linije iz grafikona, u jednoj transakciji.</summary>
public interface IScheduleRepository
{
    /// <returns>Postojeće vožnje po Id-u; nepostojeće nisu u rječniku.</returns>
    Task<IReadOnlyDictionary<Guid, ITripModel>> GetTripsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Briše vožnje <paramref name="deletes"/>, a vožnje iz <paramref name="upserts"/>
    /// dodaje (ako ne postoje) ili mijenja te im zamjenjuje vremena. Id-eve novih
    /// vožnji zadaje pozivatelj.
    /// </summary>
    Task SaveAsync(IReadOnlyList<TripStopTimes> upserts, IReadOnlyCollection<Guid> deletes, CancellationToken cancellationToken = default);
}
