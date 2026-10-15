using Terminus.Model.Common.Enums;

namespace Terminus.WebApi.Contracts.Stops;

public record StopResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    double Latitude,
    double Longitude,
    LocationType LocationType,
    Guid? ParentStationId,
    string? PlatformCode,
    WheelchairBoarding WheelchairBoarding,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
