namespace Terminus.WebApi.Contracts.Routes;

/// <summary>Redak tablice linija.</summary>
/// <param name="StopCount">Različite stanice na kojima staju vožnje linije.</param>
/// <param name="FirstDeparture">Najraniji polazak, u sekundama od početka dana voznog reda.</param>
/// <param name="LastArrival">Najkasniji dolazak, u sekundama (može biti iza 24:00).</param>
public record RouteTableRowResponse(
    RouteResponse Route,
    string AgencyName,
    int VariantCount,
    int TripCount,
    int StopCount,
    int? FirstDeparture,
    int? LastArrival);
