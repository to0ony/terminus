namespace Terminus.Model.Common;

/// <summary>Vožnja s vremenima, poredanima po rednom broju.</summary>
public sealed record TripStopTimes(ITripModel Trip, IReadOnlyList<IStopTimeModel> StopTimes);
