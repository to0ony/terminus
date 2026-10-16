namespace Terminus.Model.Common.Enums;

/// <summary>GTFS location_type.</summary>
public enum LocationType : short
{
    /// <summary>Stajalište ili peron. Jedino na njemu vožnje staju.</summary>
    StopOrPlatform = 0,

    /// <summary>Stanica (kolodvor), roditelj perona.</summary>
    Station = 1,

    /// <summary>Ulaz ili izlaz stanice; roditelj je stanica.</summary>
    Entrance = 2,

    /// <summary>Čvor unutar stanice (npr. za pathways); roditelj je stanica.</summary>
    GenericNode = 3,

    /// <summary>Mjesto ukrcavanja na peronu; roditelj je peron.</summary>
    BoardingArea = 4
}
