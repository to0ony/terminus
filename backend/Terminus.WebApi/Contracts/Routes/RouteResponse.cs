using Terminus.Model.Common.Enums;

namespace Terminus.WebApi.Contracts.Routes;

public record RouteResponse(
    Guid Id,
    Guid AgencyId,
    string Code,
    string? ShortName,
    string? LongName,
    string? Description,
    RouteType RouteType,
    string? Url,
    string? Color,
    string? TextColor,
    int? SortOrder,
    ContinuousPickupDropOff? ContinuousPickup,
    ContinuousPickupDropOff? ContinuousDropOff,
    string? NetworkId,
    TimeMode TimeMode,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
