namespace Terminus.DAL.Entities;

public class Trip : IAuditable
{
    public Guid Id { get; set; }
    public Guid RouteId { get; set; }
    public Guid ServiceCalendarId { get; set; }
    public string Code { get; set; } = null!;
    public string? Headsign { get; set; }
    public string? ShortName { get; set; }
    public short? DirectionId { get; set; }
    public string? BlockId { get; set; }
    public Guid? ShapeId { get; set; }

    /// <summary>Varijanta linije po kojoj vožnja vozi; nije dio GTFS-a.</summary>
    public Guid? VariantId { get; set; }

    /// <summary>
    /// Polazak: vožnje istog polaska u različitim tipovima dana (npr. "vozi svaki
    /// dan") imaju isti DepartureId i mijenjaju se zajedno; nije dio GTFS-a.
    /// </summary>
    public Guid? DepartureId { get; set; }

    public short WheelchairAccessible { get; set; }
    public short BikesAllowed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Route Route { get; set; } = null!;
    public ServiceCalendar ServiceCalendar { get; set; } = null!;
    public Shape? Shape { get; set; }
    public RouteVariant? Variant { get; set; }
    public ICollection<StopTime> StopTimes { get; set; } = new List<StopTime>();
}
