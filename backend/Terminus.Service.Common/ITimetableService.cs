using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Service.Common;

/// <summary>Vozni red linija i stanica za pregled u dashboardu.</summary>
public interface ITimetableService
{
    /// <summary>
    /// Različiti redoslijedi stanica kojima vožnje linije prolaze (obrasci), po
    /// smjeru i od najčešćeg. Glavni obrazac smjera je prvi.
    /// </summary>
    Task<IReadOnlyList<RoutePattern>> GetRoutePatternsAsync(Guid routeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Vozni red linije kao tablica: redci su stanice, stupci vožnje poredane po
    /// polasku. Vožnje se biraju po datumu (kalendari koji tada voze) ili po
    /// jednom kalendaru; bez oboje vrijedi današnji dan u zoni prijevoznika.
    /// </summary>
    Task<RouteTimetable> GetRouteTimetableAsync(
        Guid routeId,
        RouteTimetableQuery query,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    /// <summary>Linije koje staju na stanici; za kolodvor na bilo kojem njegovom peronu.</summary>
    Task<IReadOnlyList<IRouteModel>> GetStopRoutesAsync(Guid stopId, CancellationToken cancellationToken = default);
}

/// <param name="Date">Dan voznog reda; ne može uz <paramref name="ServiceCalendarId"/>.</param>
/// <param name="DirectionId">Samo jedan smjer; null za sve vožnje.</param>
public sealed record RouteTimetableQuery(DateOnly? Date, Guid? ServiceCalendarId, TripDirection? DirectionId);

/// <summary>Redoslijed stanica koji dijeli više vožnji iste linije.</summary>
/// <param name="Headsign">Najčešći natpis smjera među tim vožnjama.</param>
/// <param name="ShapeId">Najčešća trasa među tim vožnjama.</param>
public sealed record RoutePattern(
    TripDirection? DirectionId,
    string? Headsign,
    Guid? ShapeId,
    int TripCount,
    IReadOnlyList<IStopModel> Stops);

/// <param name="Date">Odabrani dan voznog reda, ili null kad je odabran kalendar.</param>
/// <param name="Stops">Redci tablice; stanica se ponavlja ako je vožnja obilazi dvaput.</param>
/// <param name="Calendars">Kalendari vožnji u tablici.</param>
public sealed record RouteTimetable(
    IRouteModel Route,
    DateOnly? Date,
    Guid? ServiceCalendarId,
    TripDirection? DirectionId,
    IReadOnlyList<IStopModel> Stops,
    IReadOnlyList<IServiceCalendarModel> Calendars,
    IReadOnlyList<TimetableTrip> Trips);

/// <param name="Times">Vrijeme za svaki redak tablice (isti indeks kao Stops); null ako vožnja tamo ne staje.</param>
public sealed record TimetableTrip(ITripModel Trip, IReadOnlyList<IStopTimeModel?> Times);
