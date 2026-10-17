using Terminus.DAL.Entities;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Repository.Mapping;

internal static class TripMapper
{
    public static ITripModel ToModel(Trip entity) => new TripModel
    {
        Id = entity.Id,
        RouteId = entity.RouteId,
        ServiceCalendarId = entity.ServiceCalendarId,
        Code = entity.Code,
        Headsign = entity.Headsign,
        ShortName = entity.ShortName,
        DirectionId = (TripDirection?)entity.DirectionId,
        BlockId = entity.BlockId,
        ShapeId = entity.ShapeId,
        VariantId = entity.VariantId,
        WheelchairAccessible = (WheelchairAccessible)entity.WheelchairAccessible,
        BikesAllowed = (BikesAllowed)entity.BikesAllowed,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    /// <summary>Prepisuje polja koja korisnik smije mijenjati (bez Id-a i vremenskih oznaka).</summary>
    public static void Apply(ITripModel model, Trip entity)
    {
        entity.RouteId = model.RouteId;
        entity.ServiceCalendarId = model.ServiceCalendarId;
        entity.Code = model.Code;
        entity.Headsign = model.Headsign;
        entity.ShortName = model.ShortName;
        entity.DirectionId = (short?)model.DirectionId;
        entity.BlockId = model.BlockId;
        entity.ShapeId = model.ShapeId;
        entity.VariantId = model.VariantId;
        entity.WheelchairAccessible = (short)model.WheelchairAccessible;
        entity.BikesAllowed = (short)model.BikesAllowed;
    }

    public static IStopTimeModel ToModel(StopTime entity) => new StopTimeModel
    {
        TripId = entity.TripId,
        StopSequence = entity.StopSequence,
        StopId = entity.StopId,
        ArrivalTime = entity.ArrivalTime,
        DepartureTime = entity.DepartureTime,
        StopHeadsign = entity.StopHeadsign,
        PickupType = (PickupDropOffType)entity.PickupType,
        DropOffType = (PickupDropOffType)entity.DropOffType,
        ContinuousPickup = (ContinuousPickupDropOff?)entity.ContinuousPickup,
        ContinuousDropOff = (ContinuousPickupDropOff?)entity.ContinuousDropOff,
        ShapeDistTraveled = entity.ShapeDistTraveled,
        Timepoint = entity.Timepoint
    };

    public static StopTime ToEntity(IStopTimeModel model) => new()
    {
        TripId = model.TripId,
        StopSequence = model.StopSequence,
        StopId = model.StopId,
        ArrivalTime = model.ArrivalTime,
        DepartureTime = model.DepartureTime,
        StopHeadsign = model.StopHeadsign,
        PickupType = (short)model.PickupType,
        DropOffType = (short)model.DropOffType,
        ContinuousPickup = (short?)model.ContinuousPickup,
        ContinuousDropOff = (short?)model.ContinuousDropOff,
        ShapeDistTraveled = model.ShapeDistTraveled,
        Timepoint = model.Timepoint
    };
}
