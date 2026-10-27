using System.Text.Json.Serialization;

namespace Terminus.Model.Common.Enums;

/// <summary>Stupac po kojem se slaže popis stanica.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<StopSortField>))]
public enum StopSortField
{
    Name,
    Code,
    PublicCode,
    PlatformCode,
    LocationType,
    WheelchairBoarding,
    IsActive,
    RouteCount,
    UpdatedAt
}
