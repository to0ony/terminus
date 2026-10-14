namespace Terminus.Model.Common.Enums;

/// <summary>GTFS location_type.</summary>
public enum LocationType : short
{
    /// <summary>Stajalište ili peron.</summary>
    StopOrPlatform = 0,

    /// <summary>Stanica (kolodvor), roditelj perona.</summary>
    Station = 1
}
