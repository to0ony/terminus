using System.Globalization;

namespace Terminus.Common;

/// <summary>GTFS format datuma: "YYYYMMDD".</summary>
public static class GtfsDate
{
    public static string Format(DateOnly date) =>
        date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
}
