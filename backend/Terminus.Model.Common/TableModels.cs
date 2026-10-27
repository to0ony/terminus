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

/// <summary>Linija u tablici linija, s podacima o varijantama i vožnjama.</summary>
/// <param name="StopCount">Različite stanice na kojima staju vožnje linije.</param>
/// <param name="FirstDeparture">Najraniji polazak bilo koje vožnje, u sekundama od početka dana voznog reda.</param>
/// <param name="LastArrival">Najkasniji dolazak bilo koje vožnje, u sekundama (može biti iza 24:00).</param>
public sealed record RouteTableRow(
    IRouteModel Route,
    string AgencyName,
    int VariantCount,
    int TripCount,
    int StopCount,
    int? FirstDeparture,
    int? LastArrival);
