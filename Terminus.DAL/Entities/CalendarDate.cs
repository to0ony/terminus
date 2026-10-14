namespace Terminus.DAL.Entities;

public class CalendarDate
{
    public Guid ServiceCalendarId { get; set; }
    public DateOnly Date { get; set; }
    public short ExceptionType { get; set; }

    public ServiceCalendar ServiceCalendar { get; set; } = null!;
}
