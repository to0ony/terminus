using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Service.Common;

/// <summary>
/// Karta uživo: vožnje linije jednog dana s vremenima i trasama, iz kojih
/// preglednik računa gdje je koje vozilo prema voznom redu (simulacija, ne GPS).
/// </summary>
public interface ILiveMapService
{
    /// <summary>
    /// Vožnje linije koje voze zadani dan (bez njega današnji u zoni prijevoznika),
    /// uključujući vožnje prethodnog dana koje traju iza ponoći. Vremena su sekunde
    /// od ponoći zadanog dana. 404 ako linije nema.
    /// </summary>
    Task<LiveRouteDay> GetRouteDayAsync(Guid routeId, DateOnly? date, DateTimeOffset now, CancellationToken cancellationToken = default);
}

/// <param name="TimeZone">IANA zona prijevoznika; u njoj se računa trenutno vrijeme.</param>
/// <param name="Shapes">Trase vožnji, s točkama.</param>
/// <param name="Stops">Stanice vožnji s koordinatama.</param>
public sealed record LiveRouteDay(
    IRouteModel Route,
    DateOnly Date,
    string TimeZone,
    IReadOnlyList<LiveTrip> Trips,
    IReadOnlyList<IShapeModel> Shapes,
    IReadOnlyList<IStopModel> Stops);

/// <param name="Headsign">Natpis vožnje ili naziv zadnje stanice.</param>
/// <param name="ShapeId">Trasa vožnje, inače trasa njezine varijante; null bez trase.</param>
/// <param name="PreviousDay">Vožnja prethodnog dana koja traje iza ponoći (vremena su pomaknuta za dan).</param>
/// <param name="StopTimes">Stanice s koordinatama, redom.</param>
public sealed record LiveTrip(
    Guid Id,
    string? ShortName,
    string Headsign,
    TripDirection? DirectionId,
    Guid? ShapeId,
    bool PreviousDay,
    IReadOnlyList<LiveStopTime> StopTimes);

/// <param name="ArrivalTime">Sekunde od ponoći dana karte; negativno za vožnje prethodnog dana.</param>
/// <param name="DepartureTime">Sekunde od ponoći dana karte.</param>
public sealed record LiveStopTime(Guid StopId, int ArrivalTime, int DepartureTime);
