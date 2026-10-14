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
    string? Color,
    string? TextColor,
    int? SortOrder,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
