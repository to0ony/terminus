using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Service.Gtfs.Import;

/// <summary>
/// Kalendari feeda koji više ne voze. Razdoblje iz calendar.txt samo po sebi
/// ne govori ništa: feed može imati kalendar do 2030. bez ijednog dana u tjednu,
/// sa stvarnim rasporedom u calendar_dates.txt.
/// </summary>
internal static class ExpiredCalendars
{
    /// <returns>Id-evi kalendara bez ijednog dana vožnje od <paramref name="today"/> nadalje.</returns>
    public static IReadOnlySet<Guid> Find(
        IReadOnlyList<IServiceCalendarModel> calendars,
        IReadOnlyList<ICalendarDateModel> exceptions,
        DateOnly today)
    {
        var byCalendar = exceptions.ToLookup(e => e.ServiceCalendarId);
        return calendars
            .Where(c => !RunsFrom(c, byCalendar[c.Id].ToList(), today))
            .Select(c => c.Id)
            .ToHashSet();
    }

    private static bool RunsFrom(IServiceCalendarModel calendar, IReadOnlyList<ICalendarDateModel> exceptions, DateOnly today)
    {
        if (exceptions.Any(e => e.ExceptionType == CalendarExceptionType.Added && e.Date >= today))
        {
            return true;
        }

        var removed = exceptions.Where(e => e.ExceptionType == CalendarExceptionType.Removed).Select(e => e.Date).ToHashSet();
        var from = calendar.StartDate > today ? calendar.StartDate : today;
        for (var date = from; date <= calendar.EndDate; date = date.AddDays(1))
        {
            if (ServiceCalendarRules.RunsOnWeekday(calendar, date.DayOfWeek) && !removed.Contains(date))
            {
                return true;
            }
        }

        return false;
    }
}
