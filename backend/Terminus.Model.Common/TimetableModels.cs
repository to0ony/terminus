namespace Terminus.Model.Common;

/// <summary>Vožnja s vremenima, poredanima po rednom broju.</summary>
public sealed record TripStopTimes(ITripModel Trip, IReadOnlyList<IStopTimeModel> StopTimes);

/// <summary>Sažetak vožnje za popise: prva i zadnja stanica s vremenima.</summary>
/// <param name="FirstDepartureTime">Sekunde od početka dana voznog reda; null ako vožnja nema vremena.</param>
/// <param name="LastArrivalTime">Sekunde od početka dana voznog reda; null ako vožnja nema vremena.</param>
public sealed record TripSummary(
    int StopCount,
    int? FirstDepartureTime,
    string? FirstStopName,
    int? LastArrivalTime,
    string? LastStopName);

/// <summary>Pravokutno područje karte (WGS84), rubovi uključeni.</summary>
public sealed record GeoBounds(double MinLatitude, double MinLongitude, double MaxLatitude, double MaxLongitude);
