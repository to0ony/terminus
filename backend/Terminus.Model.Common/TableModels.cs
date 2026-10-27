namespace Terminus.Model.Common;

/// <summary>Oznaka linije u retku tablice (npr. linije koje staju na stanici).</summary>
public sealed record RouteTag(Guid Id, string Code, string? ShortName, string? Color, string? TextColor);

/// <summary>Stanica u tablici stanica, s podacima koji se inače traže zasebno.</summary>
/// <param name="ParentStationName">Naziv kolodvora kojem peron pripada.</param>
/// <param name="PlatformCount">Broj perona (samo kolodvor).</param>
/// <param name="TripCount">Vožnje koje staju na stanici, u svim kalendarima; kolodvor na svojim peronima.</param>
/// <param name="Routes">Linije čije vožnje staju na stanici, redom kao u GTFS-u.</param>
public sealed record StopTableRow(
    IStopModel Stop,
    string? ParentStationName,
    int PlatformCount,
    int TripCount,
    IReadOnlyList<RouteTag> Routes);
