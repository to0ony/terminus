using Terminus.Model.Common.Enums;

namespace Terminus.Model.Common;

/// <summary>Iznimka u kalendaru za jedan datum (GTFS calendar_dates).</summary>
public interface ICalendarDateModel
{
    Guid ServiceCalendarId { get; set; }
    DateOnly Date { get; set; }
    CalendarExceptionType ExceptionType { get; set; }
}
