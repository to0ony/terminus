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

    /// <summary>Isto kao <see cref="GetNextAsync(Guid, DateTimeOffset, int, CancellationToken)"/>, za stanicu zadanu GTFS kodom.</summary>
    Task<StopDepartures> GetNextAsync(string stopCode, DateTimeOffset now, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Svi polasci sa stanice jednog dana voznog reda, poredani po vremenu. Vožnje
    /// iza ponoći pripadaju danu na koji su krenule (kao u PDF voznom redu). Bez
    /// datuma vrijedi današnji dan u zoni prijevoznika.
    /// </summary>
    Task<StopDayDepartures> GetForDayAsync(Guid stopId, DateOnly? date, DateTimeOffset now, CancellationToken cancellationToken = default);
}

/// <param name="Date">Dan voznog reda.</param>
public sealed record StopDayDepartures(IStopModel Stop, DateOnly Date, IReadOnlyList<Departure> Departures);

/// <param name="AgencyName">Naziv prvog unesenog prijevoznika.</param>
/// <param name="Now">Trenutak upita u zoni prijevoznika.</param>
public sealed record StopDepartures(IStopModel Stop, string AgencyName, DateTimeOffset Now, IReadOnlyList<Departure> Departures);

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
