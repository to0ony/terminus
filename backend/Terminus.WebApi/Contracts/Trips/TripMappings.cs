using Terminus.Common;
using Terminus.Common.Exceptions;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;

namespace Terminus.WebApi.Contracts.Trips;

public static class TripMappings
{
    // RouteId, ServiceCalendarId i Code su [Required], pa ih je model binding već provjerio.
    public static ITripModel ToModel(this TripRequest request) => new TripModel
    {
        RouteId = request.RouteId!.Value,
        ServiceCalendarId = request.ServiceCalendarId!.Value,
        Code = request.Code!,
        Headsign = request.Headsign,
        ShortName = request.ShortName,
        DirectionId = request.DirectionId,
        BlockId = request.BlockId,
        ShapeId = request.ShapeId,
        WheelchairAccessible = request.WheelchairAccessible,
        BikesAllowed = request.BikesAllowed
    };

    public static TripResponse ToResponse(this ITripModel model) => new(
        model.Id,
        model.RouteId,
        model.ServiceCalendarId,
        model.Code,
        model.Headsign,
        model.ShortName,
        model.DirectionId,
        model.BlockId,
        model.ShapeId,
        model.WheelchairAccessible,
        model.BikesAllowed,
        model.CreatedAt,
        model.UpdatedAt);

    public static TripFilter ToFilter(this TripListQuery query) => new()
    {
        Search = query.Search,
        RouteId = query.RouteId,
        ServiceCalendarId = query.ServiceCalendarId,
        DirectionId = query.DirectionId,
        Page = query.Page,
        PageSize = query.PageSize
    };

    /// <summary>
    /// Pretvara vremena iz "HH:MM:SS" u sekunde. Sve greške formata skuplja i
    /// baca odjednom kao ValidationException (400).
    /// </summary>
    public static IReadOnlyList<IStopTimeModel> ToModels(this ReplaceStopTimesRequest request)
    {
        var models = new List<IStopTimeModel>();
        var errors = new Dictionary<string, string[]>();

        for (var i = 0; i < request.StopTimes!.Count; i++)
        {
            var item = request.StopTimes[i];

            if (!GtfsTime.TryParse(item.ArrivalTime, out var arrival))
            {
                errors[$"StopTimes[{i}].ArrivalTime"] = [TimeFormatMessage];
            }

            var departure = arrival;
            if (item.DepartureTime is not null && !GtfsTime.TryParse(item.DepartureTime, out departure))
            {
                errors[$"StopTimes[{i}].DepartureTime"] = [TimeFormatMessage];
            }

            models.Add(new StopTimeModel
            {
                StopId = item.StopId!.Value,
                ArrivalTime = arrival,
                DepartureTime = departure,
                StopHeadsign = item.StopHeadsign,
                PickupType = item.PickupType,
                DropOffType = item.DropOffType,
                ContinuousPickup = item.ContinuousPickup,
                ContinuousDropOff = item.ContinuousDropOff,
                ShapeDistTraveled = item.ShapeDistTraveled,
                Timepoint = item.Timepoint
            });
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        return models;
    }

    public static StopTimeResponse ToResponse(this IStopTimeModel model) => new(
        model.StopSequence,
        model.StopId,
        GtfsTime.Format(model.ArrivalTime),
        GtfsTime.Format(model.DepartureTime),
        model.StopHeadsign,
        model.PickupType,
        model.DropOffType,
        model.ContinuousPickup,
        model.ContinuousDropOff,
        model.ShapeDistTraveled,
        model.Timepoint);

    private const string TimeFormatMessage = "Vrijeme mora biti u obliku HH:MM:SS, npr. 06:15:00 ili 25:30:00.";
}
