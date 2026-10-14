namespace Terminus.DAL.Entities;

public class Agency : IAuditable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Url { get; set; }
    public string Timezone { get; set; } = "Europe/Zagreb";
    public string Lang { get; set; } = "hr";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Stop> Stops { get; set; } = new List<Stop>();
    public ICollection<Route> Routes { get; set; } = new List<Route>();
    public ICollection<ServiceCalendar> ServiceCalendars { get; set; } = new List<ServiceCalendar>();
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}
