using Terminus.Model.Common.Enums;

namespace Terminus.Model.Common;

/// <summary>Dopuštene vrijednosti GTFS route_type.</summary>
public static class RouteTypes
{
    /// <summary>Raspon proširenih vrijednosti (Hierarchical Vehicle Types).</summary>
    public const short ExtendedMin = 100;

    public const short ExtendedMax = 1799;

    /// <returns>true za osnovnu vrijednost ili vrijednost iz proširenog raspona.</returns>
    public static bool IsValid(RouteType routeType) =>
        Enum.IsDefined(routeType) || (short)routeType is >= ExtendedMin and <= ExtendedMax;
}
