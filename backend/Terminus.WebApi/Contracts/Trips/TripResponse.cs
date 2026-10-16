using Terminus.Model.Common.Enums;

namespace Terminus.WebApi.Contracts.Trips;

public record TripResponse(
    Guid Id,
    Guid RouteId,
    Guid ServiceCalendarId,
    string Code,
    string? Headsign,
    string? ShortName,
    TripDirection? DirectionId,
    string? BlockId,
    Guid? ShapeId,
    WheelchairAccessible WheelchairAccessible,
    BikesAllowed BikesAllowed,
    DateTime CreatedAt,
    DateTime UpdatedAt);
