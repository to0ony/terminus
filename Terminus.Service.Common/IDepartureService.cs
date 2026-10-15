using Terminus.Model.Common;

namespace Terminus.Service.Common;

/// <summary>Izračun polazaka sa stanice iz voznog reda.</summary>
public interface IDepartureService
{
    /// <summary>
    /// Sljedeći polasci od trenutka <paramref name="now"/> u idućih 24 h, poredani
    /// po vremenu. Za kolodvor se vraćaju polasci sa svih njegovih perona.
    /// </summary>
    /// <param name="limit">Najviše polazaka (1 do <c>DepartureService.MaxLimit</c>).</param>
    Task<StopDepartures> GetNextAsync(Guid stopId, DateTimeOffset now, int limit, CancellationToken cancellationToken = default);
}

/// <param name="Now">Trenutak upita u zoni prijevoznika.</param>
public sealed record StopDepartures(IStopModel Stop, DateTimeOffset Now, IReadOnlyList<Departure> Departures);

/// <summary>Jedan polazak sa stvarnim datumom i vremenom.</summary>
/// <param name="ServiceDate">Dan voznog reda kojem vožnja pripada (za vožnje iza ponoći dan ranije).</param>
/// <param name="DepartureTime">Stvarno vrijeme polaska u zoni prijevoznika.</param>
public sealed record Departure(
    Guid TripId,
    Guid RouteId,
    string? RouteShortName,
    string? RouteLongName,
    string? RouteColor,
    string? RouteTextColor,
    string Headsign,
    Guid StopId,
    string? PlatformCode,
    DateOnly ServiceDate,
    DateTimeOffset DepartureTime);
