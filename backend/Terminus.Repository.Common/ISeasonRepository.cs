using Terminus.Model.Common;

namespace Terminus.Repository.Common;

/// <summary>Sezone voznog reda s tipovima dana i danima koji odstupaju.</summary>
public interface ISeasonRepository
{
    /// <returns>Sve sezone, poredane po početku.</returns>
    Task<IReadOnlyList<SeasonState>> ListAsync(CancellationToken cancellationToken = default);

    Task<SeasonState?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <returns>Broj vožnji po kalendaru; kalendari bez vožnji nisu u rječniku.</returns>
    Task<IReadOnlyDictionary<Guid, int>> CountTripsAsync(IReadOnlyCollection<Guid> calendarIds, CancellationToken cancellationToken = default);

    /// <returns>Sezona koja se preklapa s razdobljem (osim <paramref name="exceptId"/>) ili null.</returns>
    Task<ITimetableSeasonModel?> FindOverlappingAsync(DateOnly start, DateOnly end, Guid? exceptId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprema cijelu sezonu u jednoj transakciji: sezonu, tipove dana (novi se
    /// dodaju, nenavedeni brišu), dane i iznimke kalendara sezone. Id-eve novih
    /// zapisa zadaje pozivatelj.
    /// </summary>
    /// <param name="copyTrips">
    /// Kalendar → tip dana ove sezone: vožnje tih kalendara (s vremenima) kopiraju se u
    /// odgovarajući tip dana, u istoj transakciji. Koristi se kod kopiranja sezone.
    /// </param>
    Task SaveAsync(
        SeasonState state,
        IReadOnlyList<ICalendarDateModel> calendarDates,
        IReadOnlyDictionary<Guid, Guid>? copyTrips = null,
        CancellationToken cancellationToken = default);

    /// <returns>false ako sezona ne postoji.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
