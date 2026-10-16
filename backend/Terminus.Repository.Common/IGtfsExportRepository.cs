using Terminus.Model.Common;

namespace Terminus.Repository.Common;

/// <summary>
/// Čitanje svih podataka za GTFS izvoz, bez straničenja.
/// Odabir što ulazi u feed radi servis.
/// </summary>
public interface IGtfsExportRepository
{
    /// <returns>Svi prijevoznici, od najranije unesenog.</returns>
    Task<IReadOnlyList<IAgencyModel>> GetAgenciesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IStopModel>> GetStopsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IRouteModel>> GetRoutesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IServiceCalendarModel>> GetServiceCalendarsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ICalendarDateModel>> GetCalendarDatesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ITripModel>> GetTripsAsync(CancellationToken cancellationToken = default);

    /// <returns>Vremena svih vožnji, poredana po vožnji i rednom broju.</returns>
    Task<IReadOnlyList<IStopTimeModel>> GetStopTimesAsync(CancellationToken cancellationToken = default);

    /// <returns>Sve trase s točkama, poredane po kodu.</returns>
    Task<IReadOnlyList<IShapeModel>> GetShapesAsync(CancellationToken cancellationToken = default);

    /// <returns>Sve cijene s pravilima, poredane po kodu.</returns>
    Task<IReadOnlyList<IFareModel>> GetFaresAsync(CancellationToken cancellationToken = default);

    /// <returns>Podaci o izdavaču ili null.</returns>
    Task<IFeedInfoModel?> GetFeedInfoAsync(CancellationToken cancellationToken = default);
}
