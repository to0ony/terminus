using System.Text.Json.Serialization;

namespace Terminus.Model.Common.Enums;

/// <summary>Stupac po kojem se slaže popis linija.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RouteSortField>))]
public enum RouteSortField
{
    /// <summary>Kao u GTFS-u: route_sort_order, pa oznaka (2 prije 11) i naziv.</summary>
    Default,
    Code,
    ShortName,
    LongName,
    RouteType,
    Agency,
    IsActive,
    VariantCount,
    TripCount,
    UpdatedAt
}
