using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Model;

public class TimetableSeasonModel : ITimetableSeasonModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? HolidayCountry { get; set; }
    public SeasonDayMode HolidayMode { get; set; }
    public Guid? HolidayCalendarId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
