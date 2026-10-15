namespace Terminus.WebApi.Contracts.Departures;

/// <param name="Now">Trenutno vrijeme u zoni prijevoznika.</param>
public record DeparturesResponse(
    DepartureStopResponse Stop,
    string AgencyName,
    DateTimeOffset Now,
    IReadOnlyList<DepartureResponse> Departures);

/// <summary>Javni podaci stanice; bez internih Id-eva.</summary>
public record DepartureStopResponse(string Code, string Name);

/// <param name="RouteName">Kratka oznaka linije, a ako je nema, puni naziv.</param>
/// <param name="Platform">Oznaka perona (kod kolodvora).</param>
/// <param name="DepartureTime">Polazak u zoni prijevoznika.</param>
/// <param name="MinutesUntil">Cijele minute do polaska (zaokruženo nadolje).</param>
public record DepartureResponse(
    string? RouteName,
    string? RouteColor,
    string? RouteTextColor,
    string Headsign,
    string? Platform,
    DateTimeOffset DepartureTime,
    int MinutesUntil);
