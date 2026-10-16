namespace Terminus.Model.Common;

/// <summary>Vožnja s vremenima, poredanima po rednom broju.</summary>
public sealed record TripStopTimes(ITripModel Trip, IReadOnlyList<IStopTimeModel> StopTimes);

/// <summary>Pravokutno područje karte (WGS84), rubovi uključeni.</summary>
public sealed record GeoBounds(double MinLatitude, double MinLongitude, double MaxLatitude, double MaxLongitude);
