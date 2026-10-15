using Terminus.Model.Common;

namespace Terminus.Repository.Common;

/// <summary>
/// Vremena vožnje po stanicama. Ne provjerava postoji li vožnja; to radi servis
/// dohvatom vožnje prije svake operacije.
/// </summary>
public interface IStopTimeRepository
{
    /// <returns>Vremena poredana po rednom broju stanice.</returns>
    Task<IReadOnlyList<IStopTimeModel>> GetByTripAsync(Guid tripId, CancellationToken cancellationToken = default);

    /// <summary>
    /// U jednoj transakciji briše sva postojeća vremena vožnje, sprema nova i
    /// osvježava UpdatedAt vožnje.
    /// </summary>
    Task<IReadOnlyList<IStopTimeModel>> ReplaceForTripAsync(
        Guid tripId,
        IReadOnlyList<IStopTimeModel> stopTimes,
        CancellationToken cancellationToken = default);
}
