namespace Terminus.WebApi.Contracts.Departures;

/// <summary>Query parametri za polaske sa stanice.</summary>
public class DeparturesQuery
{
    /// <summary>Koliko polazaka vratiti (1 do 30).</summary>
    public int Limit { get; set; } = 10;

    /// <summary>Koliko upravo otišlih polazaka vratiti uz sljedeće (0 do 5).</summary>
    public int Past { get; set; }
}
