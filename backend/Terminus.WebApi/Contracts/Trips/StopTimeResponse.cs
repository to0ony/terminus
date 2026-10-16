using Terminus.Model.Common.Enums;

namespace Terminus.WebApi.Contracts.Trips;

public record StopTimeResponse(
    int StopSequence,
    Guid StopId,
    string ArrivalTime,
    string DepartureTime,
    string? StopHeadsign,
    PickupDropOffType PickupType,
    PickupDropOffType DropOffType,
    ContinuousPickupDropOff? ContinuousPickup,
    ContinuousPickupDropOff? ContinuousDropOff,
    double? ShapeDistTraveled,
    bool Timepoint);
