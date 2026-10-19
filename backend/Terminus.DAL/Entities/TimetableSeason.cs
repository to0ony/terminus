namespace Terminus.DAL.Entities;

/// <summary>
/// Sezona voznog reda: razdoblje u kojem vrijede njezini tipovi dana (kalendari)
/// i pravilo za praznike. Iznimke kalendara (calendar_dates) izračunavaju se iz
/// dana sezone (<see cref="SeasonDay"/>).
/// </summary>
public class TimetableSeason : IAuditable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>ISO kod države za praznike (npr. "HR"); null bez automatskih praznika.</summary>
    public string? HolidayCountry { get; set; }

    /// <summary>Kako se vozi na praznike (SeasonDayMode).</summary>
    public short HolidayMode { get; set; }

    /// <summary>Tip dana koji vozi na praznike kad je HolidayMode "vozi kao".</summary>
    public Guid? HolidayCalendarId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ServiceCalendar> Calendars { get; set; } = new List<ServiceCalendar>();
    public ICollection<SeasonDay> Days { get; set; } = new List<SeasonDay>();
    public ICollection<SeasonPeriod> Periods { get; set; } = new List<SeasonPeriod>();
}
