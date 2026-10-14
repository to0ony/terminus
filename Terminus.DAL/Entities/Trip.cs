namespace Terminus.DAL.Entities;

public class Trip : IAuditable
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public Guid RouteId { get; set; }
    public Guid ServiceCalendarId { get; set; }
    public string Code { get; set; } = null!;
    public string? Headsign { get; set; }
    public string? ShortName { get; set; }
    public short? DirectionId { get; set; }
    public string? BlockId { get; set; }
    public short WheelchairAccessible { get; set; }
    public short BikesAllowed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Agency Agency { get; set; } = null!;
    public Route Route { get; set; } = null!;
    public ServiceCalendar ServiceCalendar { get; set; } = null!;
    public ICollection<StopTime> StopTimes { get; set; } = new List<StopTime>();
}
