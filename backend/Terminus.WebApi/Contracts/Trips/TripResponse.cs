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
    Guid? VariantId,
    WheelchairAccessible WheelchairAccessible,
    BikesAllowed BikesAllowed,
    TripSummaryResponse Summary,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>Prva i zadnja stanica vožnje; vremena su null dok vožnja nema stanica.</summary>
/// <param name="FirstDepartureTime">HH:MM:SS, može biti iznad 24 h.</param>
/// <param name="LastArrivalTime">HH:MM:SS, može biti iznad 24 h.</param>
public record TripSummaryResponse(
    int StopCount,
    string? FirstDepartureTime,
    string? FirstStopName,
    string? LastArrivalTime,
    string? LastStopName);
