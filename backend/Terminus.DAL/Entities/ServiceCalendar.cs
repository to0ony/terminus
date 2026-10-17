namespace Terminus.DAL.Entities;

public class ServiceCalendar : IAuditable
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool Monday { get; set; }
    public bool Tuesday { get; set; }
    public bool Wednesday { get; set; }
    public bool Thursday { get; set; }
    public bool Friday { get; set; }
    public bool Saturday { get; set; }
    public bool Sunday { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>Sezona kojoj kalendar pripada kao tip dana; null za uvezene i ručno unesene kalendare.</summary>
    public Guid? SeasonId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public TimetableSeason? Season { get; set; }
    public ICollection<CalendarDate> CalendarDates { get; set; } = new List<CalendarDate>();
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}
