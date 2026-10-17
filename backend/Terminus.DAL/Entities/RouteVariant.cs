namespace Terminus.DAL.Entities;

/// <summary>
/// Varijanta linije: redoslijed stanica jednog smjera s vremenima vožnje (npr.
/// glavna, skraćena, iz garaže). Nije dio GTFS-a; vožnje je koriste kao
/// predložak, a u GTFS izvoz idu vremena samih vožnji.
/// </summary>
public class RouteVariant : IAuditable
{
    public Guid Id { get; set; }
    public Guid RouteId { get; set; }
    public string Name { get; set; } = null!;
    public short? DirectionId { get; set; }
    public string? Headsign { get; set; }
    public Guid? ShapeId { get; set; }

    /// <summary>Redoslijed prikaza; prva varijanta smjera je glavna.</summary>
    public int SortOrder { get; set; }

    /// <summary>VariantSource: ručno ili iz GTFS uvoza.</summary>
    public short Source { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Route Route { get; set; } = null!;
    public Shape? Shape { get; set; }
    public ICollection<RouteVariantStop> Stops { get; set; } = new List<RouteVariantStop>();
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}

/// <summary>Stanica varijante s vremenom vožnje od prethodne stanice i stajanjem.</summary>
public class RouteVariantStop
{
    public Guid VariantId { get; set; }

    /// <summary>Redni broj u varijanti (1, 2, 3 ...).</summary>
    public int Sequence { get; set; }

    public Guid StopId { get; set; }

    /// <summary>Sekunde od polaska s prethodne stanice do dolaska na ovu; 0 za prvu.</summary>
    public int TravelTime { get; set; }

    /// <summary>Sekunde stajanja (od dolaska do polaska).</summary>
    public int DwellTime { get; set; }

    public RouteVariant Variant { get; set; } = null!;
    public Stop Stop { get; set; } = null!;
}
