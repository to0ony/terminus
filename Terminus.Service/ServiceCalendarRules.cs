using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Service;

/// <summary>Vozi li kalendar na zadani datum (GTFS calendar.txt + calendar_dates.txt).</summary>
public static class ServiceCalendarRules
{
    /// <summary>
    /// Iznimka za datum ima prednost: "vozi" uključuje datum i izvan razdoblja ili
    /// na neoznačen dan, "ne vozi" ga isključuje. Bez iznimke kalendar vozi ako je
    /// datum u razdoblju i dan u tjednu je označen.
    /// </summary>
    /// <param name="exceptions">Iznimke; one drugih kalendara i datuma se zanemaruju.</param>
    public static bool RunsOn(IServiceCalendarModel calendar, IEnumerable<ICalendarDateModel> exceptions, DateOnly date)
    {
        var exception = exceptions.FirstOrDefault(e => e.ServiceCalendarId == calendar.Id && e.Date == date);
        if (exception is not null)
        {
            return exception.ExceptionType == CalendarExceptionType.Added;
        }

        return date >= calendar.StartDate
            && date <= calendar.EndDate
            && RunsOnWeekday(calendar, date.DayOfWeek);
    }

    /// <returns>Id-evi kalendara koji voze na zadani datum.</returns>
    public static IReadOnlySet<Guid> ActiveOn(
        IEnumerable<IServiceCalendarModel> calendars,
        IReadOnlyCollection<ICalendarDateModel> exceptions,
        DateOnly date) =>
        calendars.Where(c => RunsOn(c, exceptions, date)).Select(c => c.Id).ToHashSet();

    public static bool RunsOnWeekday(IServiceCalendarModel calendar, DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => calendar.Monday,
        DayOfWeek.Tuesday => calendar.Tuesday,
        DayOfWeek.Wednesday => calendar.Wednesday,
        DayOfWeek.Thursday => calendar.Thursday,
        DayOfWeek.Friday => calendar.Friday,
        DayOfWeek.Saturday => calendar.Saturday,
        DayOfWeek.Sunday => calendar.Sunday,
        _ => false
    };
}
