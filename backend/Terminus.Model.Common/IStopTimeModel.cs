using Terminus.Model.Common.Enums;

namespace Terminus.Model.Common;

/// <summary>Dolazak i polazak vožnje na jednoj stanici (GTFS stop_times).</summary>
public interface IStopTimeModel
{
    Guid TripId { get; set; }

    /// <summary>Redni broj stanice na vožnji (1, 2, 3 ...).</summary>
    int StopSequence { get; set; }

    Guid StopId { get; set; }

    /// <summary>Sekunde od ponoći dana vožnje (može biti iznad 24 h).</summary>
    int ArrivalTime { get; set; }

    /// <summary>Sekunde od ponoći dana vožnje (može biti iznad 24 h).</summary>
    int DepartureTime { get; set; }

    string? StopHeadsign { get; set; }
    PickupDropOffType PickupType { get; set; }
    PickupDropOffType DropOffType { get; set; }

    /// <summary>Null znači vrijednost linije (GTFS continuous_pickup).</summary>
    ContinuousPickupDropOff? ContinuousPickup { get; set; }

    ContinuousPickupDropOff? ContinuousDropOff { get; set; }

    /// <summary>Prijeđena udaljenost duž trase, u istim jedinicama kao trasa (GTFS shape_dist_traveled).</summary>
    double? ShapeDistTraveled { get; set; }

    /// <summary>true = točno vrijeme, false = okvirno.</summary>
    bool Timepoint { get; set; }
}
