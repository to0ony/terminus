namespace Terminus.Model.Common.Enums;

/// <summary>GTFS direction_id: razlikuje dva smjera iste linije.</summary>
public enum TripDirection : short
{
    /// <summary>Jedan smjer (npr. "tamo").</summary>
    Outbound = 0,

    /// <summary>Suprotan smjer (npr. "natrag").</summary>
    Inbound = 1
}
