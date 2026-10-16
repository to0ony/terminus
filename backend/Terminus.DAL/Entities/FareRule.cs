namespace Terminus.DAL.Entities;

/// <summary>GTFS fare_rules. Nema prirodnog ključa jer su sva polja osim cijene neobavezna.</summary>
public class FareRule
{
    public Guid Id { get; set; }
    public Guid FareId { get; set; }

    /// <summary>Redoslijed pravila unutar cijene.</summary>
    public int Sequence { get; set; }

    public Guid? RouteId { get; set; }
    public string? OriginId { get; set; }
    public string? DestinationId { get; set; }
    public string? ContainsId { get; set; }

    public Fare Fare { get; set; } = null!;
    public Route? Route { get; set; }
}
