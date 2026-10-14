using Terminus.Model.Common;

namespace Terminus.Repository.Common;

/// <summary>
/// Čitanje svih podataka agencije za GTFS izvoz, bez straničenja.
/// Odabir što ulazi u feed radi servis.
/// </summary>
public interface IGtfsExportRepository
{
    Task<IAgencyModel?> GetAgencyAsync(Guid agencyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IStopModel>> GetStopsAsync(Guid agencyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IRouteModel>> GetRoutesAsync(Guid agencyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IServiceCalendarModel>> GetServiceCalendarsAsync(Guid agencyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ICalendarDateModel>> GetCalendarDatesAsync(Guid agencyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ITripModel>> GetTripsAsync(Guid agencyId, CancellationToken cancellationToken = default);

    /// <returns>Vremena svih vožnji agencije, poredana po vožnji i rednom broju.</returns>
    Task<IReadOnlyList<IStopTimeModel>> GetStopTimesAsync(Guid agencyId, CancellationToken cancellationToken = default);
}
