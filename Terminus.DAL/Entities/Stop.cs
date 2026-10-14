namespace Terminus.DAL.Entities;

public class Stop : IAuditable
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public short LocationType { get; set; }
    public Guid? ParentStationId { get; set; }
    public string? PlatformCode { get; set; }
    public short WheelchairBoarding { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Agency Agency { get; set; } = null!;
    public Stop? ParentStation { get; set; }
    public ICollection<Stop> ChildStops { get; set; } = new List<Stop>();
    public ICollection<StopTime> StopTimes { get; set; } = new List<StopTime>();
}
