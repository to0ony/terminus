namespace Terminus.Model.Common;

/// <summary>
/// Polazak iz voznog reda za jedan dan voznog reda, prije pretvorbe u stvarno
/// vrijeme. Smjer (Headsign) je već odabran: stop_headsign, zatim trip_headsign,
/// zatim naziv zadnje stanice vožnje.
/// </summary>
/// <param name="DepartureTime">Sekunde od početka dana voznog reda (može biti iznad 24 h).</param>
public sealed record ScheduledDeparture(
    Guid TripId,
    Guid ServiceCalendarId,
    Guid RouteId,
    string? RouteShortName,
    string? RouteLongName,
    string? RouteColor,
    string? RouteTextColor,
    string Headsign,
    Guid StopId,
    string? PlatformCode,
    int DepartureTime);
