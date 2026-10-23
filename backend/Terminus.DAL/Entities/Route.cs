namespace Terminus.DAL.Entities;

public class Route : IAuditable
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public string Code { get; set; } = null!;
    public string? ShortName { get; set; }
    public string? LongName { get; set; }
    public string? Description { get; set; }
    public short RouteType { get; set; } = 3;
    public string? Url { get; set; }
    public string? Color { get; set; }
    public string? TextColor { get; set; }
    public int? SortOrder { get; set; }
    public short? ContinuousPickup { get; set; }
    public short? ContinuousDropOff { get; set; }
    public string? NetworkId { get; set; }
    /// <summary>TimeMode: točna ili okvirna vremena na međustanicama.</summary>
    public short TimeMode { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Agency Agency { get; set; } = null!;
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
    public ICollection<FareRule> FareRules { get; set; } = new List<FareRule>();
}
