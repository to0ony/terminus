namespace Terminus.DAL.Entities;

public class StopTime
{
    public Guid TripId { get; set; }
    public int StopSequence { get; set; }
    public Guid StopId { get; set; }

    /// <summary>Sekunde od ponoći (GTFS dopušta više od 24h).</summary>
    public int ArrivalTime { get; set; }

    /// <summary>Sekunde od ponoći (GTFS dopušta više od 24h).</summary>
    public int DepartureTime { get; set; }

    public string? StopHeadsign { get; set; }
    public short PickupType { get; set; }
    public short DropOffType { get; set; }
    public bool Timepoint { get; set; } = true;

    public Trip Trip { get; set; } = null!;
    public Stop Stop { get; set; } = null!;
}
