using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Model;

public class StopTimeModel : IStopTimeModel
{
    public Guid TripId { get; set; }
    public int StopSequence { get; set; }
    public Guid StopId { get; set; }
    public int ArrivalTime { get; set; }
    public int DepartureTime { get; set; }
    public string? StopHeadsign { get; set; }
    public PickupDropOffType PickupType { get; set; }
    public PickupDropOffType DropOffType { get; set; }
    public ContinuousPickupDropOff? ContinuousPickup { get; set; }
    public ContinuousPickupDropOff? ContinuousDropOff { get; set; }
    public double? ShapeDistTraveled { get; set; }
    public bool Timepoint { get; set; } = true;
}
