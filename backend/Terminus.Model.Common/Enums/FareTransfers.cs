namespace Terminus.Model.Common.Enums;

/// <summary>GTFS fare_attributes.transfers. Bez vrijednosti (null) presjedanja su neograničena.</summary>
public enum FareTransfers : short
{
    /// <summary>Presjedanje nije dopušteno.</summary>
    None = 0,

    /// <summary>Jedno presjedanje.</summary>
    Once = 1,

    /// <summary>Dva presjedanja.</summary>
    Twice = 2
}
