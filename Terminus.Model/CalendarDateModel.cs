using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Model;

public class CalendarDateModel : ICalendarDateModel
{
    public Guid ServiceCalendarId { get; set; }
    public DateOnly Date { get; set; }
    public CalendarExceptionType ExceptionType { get; set; }
}
