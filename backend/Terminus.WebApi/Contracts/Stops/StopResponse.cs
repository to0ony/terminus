using Terminus.Model.Common.Enums;

namespace Terminus.WebApi.Contracts.Stops;

public record StopResponse(
    Guid Id,
    string Code,
    string? PublicCode,
    string Name,
    string? TtsName,
    string? Description,
    double? Latitude,
    double? Longitude,
    string? ZoneId,
    string? Url,
    LocationType LocationType,
    Guid? ParentStationId,
    string? Timezone,
    string? PlatformCode,
    WheelchairBoarding WheelchairBoarding,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
