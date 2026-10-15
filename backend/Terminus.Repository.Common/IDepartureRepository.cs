using Terminus.Model.Common;

namespace Terminus.Repository.Common;

/// <summary>Čitanje voznog reda za izračun polazaka. Koje kalendare uključiti odlučuje servis.</summary>
public interface IDepartureRepository
{
    /// <returns>Prvi uneseni prijevoznik (njegova zona vrijedi za sve) ili null ako prijevoznika nema.</returns>
    Task<IAgencyModel?> GetPrimaryAgencyAsync(CancellationToken cancellationToken = default);

    /// <returns>Peroni kolodvora.</returns>
    Task<IReadOnlyList<IStopModel>> GetPlatformsAsync(Guid stationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IServiceCalendarModel>> GetCalendarsAsync(CancellationToken cancellationToken = default);

    /// <returns>Iznimke svih kalendara u razdoblju (uključivo).</returns>
    Task<IReadOnlyList<ICalendarDateModel>> GetCalendarDatesAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Polasci sa stanica u rasponu vremena (sekunde, uključivo), samo za vožnje
    /// zadanih kalendara na aktivnim linijama. Izostavlja zadnju stanicu vožnje i
    /// stanice bez ukrcavanja.
    /// </summary>
    /// <returns>Najviše <paramref name="limit"/> polazaka, poredanih po vremenu.</returns>
    Task<IReadOnlyList<ScheduledDeparture>> GetScheduledAsync(
        IReadOnlyCollection<Guid> stopIds,
        IReadOnlyCollection<Guid> serviceCalendarIds,
        int fromSeconds,
        int toSeconds,
        int limit,
        CancellationToken cancellationToken = default);
}
