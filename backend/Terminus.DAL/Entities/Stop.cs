namespace Terminus.DAL.Entities;

public class Stop : IAuditable
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string? PublicCode { get; set; }
    public string Name { get; set; } = null!;
    public string? TtsName { get; set; }
    public string? Description { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? ZoneId { get; set; }
    public string? Url { get; set; }
    public short LocationType { get; set; }
    public Guid? ParentStationId { get; set; }
    public string? Timezone { get; set; }
    public string? PlatformCode { get; set; }
    public short WheelchairBoarding { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Stop? ParentStation { get; set; }
    public ICollection<Stop> ChildStops { get; set; } = new List<Stop>();
    public ICollection<StopTime> StopTimes { get; set; } = new List<StopTime>();
}
