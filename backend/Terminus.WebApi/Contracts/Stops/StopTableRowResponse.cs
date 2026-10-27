using Terminus.WebApi.Contracts.Routes;

namespace Terminus.WebApi.Contracts.Stops;

/// <summary>Redak tablice stanica.</summary>
/// <param name="ParentStationName">Naziv kolodvora kojem peron pripada.</param>
/// <param name="PlatformCount">Broj perona (samo kolodvor).</param>
/// <param name="TripCount">Vožnje koje staju na stanici, u svim kalendarima; kolodvor na svojim peronima.</param>
public record StopTableRowResponse(
    StopResponse Stop,
    string? ParentStationName,
    int PlatformCount,
    int TripCount,
    IReadOnlyList<RouteTagResponse> Routes);
