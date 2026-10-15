using Terminus.Model.Common;

namespace Terminus.Repository.Common;

/// <summary>
/// Iznimke kalendara. Ne provjerava postoji li kalendar; to radi servis
/// dohvatom kalendara prije svake operacije.
/// </summary>
public interface ICalendarDateRepository
{
    /// <returns>Iznimke poredane po datumu.</returns>
    Task<IReadOnlyList<ICalendarDateModel>> GetByCalendarAsync(Guid serviceCalendarId, CancellationToken cancellationToken = default);

    /// <summary>Dodaje iznimku ili mijenja postojeću za isti datum.</summary>
    Task<ICalendarDateModel> UpsertAsync(ICalendarDateModel calendarDate, CancellationToken cancellationToken = default);

    /// <returns>false ako iznimka za taj datum ne postoji.</returns>
    Task<bool> DeleteAsync(Guid serviceCalendarId, DateOnly date, CancellationToken cancellationToken = default);
}
