using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Service;

/// <summary>
/// Pretvara sezonu u GTFS: tipovi dana su kalendari s danima u tjednu i
/// razdobljem sezone, a dani koji odstupaju postaju iznimke (calendar_dates).
/// </summary>
internal static class SeasonCalendarBuilder
{
    /// <summary>
    /// Iznimke za dane sezone. Za "vozi kao X" tip dana koji inače vozi taj dan
    /// dobiva "ne vozi", a X "vozi"; za "ne vozi" svi tipovi koji inače voze
    /// dobivaju "ne vozi". Dani izvan razdoblja se zanemaruju.
    /// </summary>
    public static IReadOnlyList<ICalendarDateModel> CalendarDates(
        ITimetableSeasonModel season,
        IReadOnlyList<IServiceCalendarModel> dayTypes,
        IReadOnlyList<SeasonDay> days)
    {
        var result = new List<ICalendarDateModel>();

        foreach (var day in days.Where(d => d.Date >= season.StartDate && d.Date <= season.EndDate))
        {
            foreach (var dayType in dayTypes)
            {
                var regular = ServiceCalendarRules.RunsOnWeekday(dayType, day.Date.DayOfWeek);
                var runs = day.Mode switch
                {
                    SeasonDayMode.RunsAs => day.CalendarId == dayType.Id,
                    SeasonDayMode.NoService => false,
                    _ => regular
                };

                if (regular != runs)
                {
                    result.Add(new CalendarDateModel
                    {
                        ServiceCalendarId = dayType.Id,
                        Date = day.Date,
                        ExceptionType = runs ? CalendarExceptionType.Added : CalendarExceptionType.Removed
                    });
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Ponovno primjenjuje pravilo za praznike: stari dani iz praznika se brišu,
    /// a svaki praznik u razdoblju dobiva pravilo sezone. Ručna iznimka za isti
    /// datum ima prednost. Praznik se pamti i kad ništa ne mijenja (npr. pada u
    /// nedjelju), da se vidi u kalendaru.
    /// </summary>
    public static IReadOnlyList<SeasonDay> ApplyHolidays(
        ITimetableSeasonModel season,
        IReadOnlyList<SeasonDay> days,
        IReadOnlyList<PublicHoliday> holidays)
    {
        var manual = days
            .Where(d => d.Source == SeasonDaySource.Manual && d.Date >= season.StartDate && d.Date <= season.EndDate)
            .ToList();
        var manualDates = manual.Select(d => d.Date).ToHashSet();

        var fromHolidays = holidays
            .Where(h => h.Date >= season.StartDate && h.Date <= season.EndDate && !manualDates.Contains(h.Date))
            .DistinctBy(h => h.Date)
            .Select(h => new SeasonDay(
                h.Date,
                season.HolidayMode,
                season.HolidayMode == SeasonDayMode.RunsAs ? season.HolidayCalendarId : null,
                SeasonDaySource.Holiday,
                h.Name));

        return manual.Concat(fromHolidays).OrderBy(d => d.Date).ToList();
    }
}
