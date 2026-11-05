using System.Globalization;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Service.Gtfs.Import;

/// <summary>
/// Pretvara kalendare feeda u sezonu voznog reda. Za svaki dan u tjednu najčešći
/// kalendar postaje njegov tip dana, a ostali su tipovi dana bez dana u tjednu
/// koji voze kroz iznimke ("vozi kao"). Sezona vozi točno iste dane kao feed.
/// Moguće je samo ako svakog dana vozi najviše jedan kalendar, jer tip dana
/// sezone isključuje ostale.
/// </summary>
internal static class FeedSeasonBuilder
{
    /// <param name="from">Prvi dan sezone; raniji dani feeda se zanemaruju.</param>
    public static FeedSeason Build(GtfsFeedData feed, DateOnly? from)
    {
        // Kalendari bez vožnji ne utječu na sezonu i ostaju izvan nje.
        var used = feed.Trips.Select(t => t.ServiceCalendarId).ToHashSet();
        var calendars = feed.Calendars.Where(c => used.Contains(c.Id)).ToList();
        if (calendars.Count == 0)
        {
            return FeedSeason.Impossible("Feed nema kalendara s vožnjama.");
        }

        if (calendars.Count > SeasonService.MaxDayTypes)
        {
            return FeedSeason.Impossible($"Feed ima {calendars.Count} kalendara, a sezona najviše {SeasonService.MaxDayTypes} tipova dana.");
        }

        var exceptions = feed.CalendarDates.ToLookup(d => d.ServiceCalendarId);
        var running = new Dictionary<DateOnly, IServiceCalendarModel>();
        foreach (var calendar in calendars)
        {
            foreach (var date in ServiceDates(calendar, exceptions[calendar.Id].ToList()).Where(d => from is null || d >= from))
            {
                if (!running.TryAdd(date, calendar))
                {
                    return FeedSeason.Impossible($"{Format(date)} vozi više kalendara odjednom, a sezona ima jedan tip dana po danu.");
                }
            }
        }

        if (running.Count == 0)
        {
            return FeedSeason.Impossible("Feed nema nijednog dana vožnje.");
        }

        var start = running.Keys.Min();
        var end = running.Keys.Max();
        if (end.DayNumber - start.DayNumber + 1 > SeasonService.MaxDays)
        {
            return FeedSeason.Impossible($"Feed traje dulje od {SeasonService.MaxDays} dana, koliko može trajati sezona.");
        }

        var regular = running
            .GroupBy(r => r.Key.DayOfWeek)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(r => r.Value).OrderByDescending(c => c.Count()).ThenBy(c => c.Key.Code, StringComparer.Ordinal).First().Key);

        var season = new TimetableSeasonModel
        {
            Id = Guid.CreateVersion7(),
            Name = $"Vozni red {Format(start)} – {Format(end)}",
            StartDate = start,
            EndDate = end,
            HolidayMode = SeasonDayMode.Regular
        };

        var dayTypes = calendars.Select(c => DayType(c, season, regular)).ToList();
        var days = Days(start, end, running, regular, dayTypes);
        var calendarDates = SeasonCalendarBuilder.CalendarDates(season, dayTypes, days);

        return new FeedSeason(
            null,
            season,
            dayTypes,
            calendarDates,
            days,
            feed.Calendars.Where(c => !used.Contains(c.Id)).ToList(),
            feed.CalendarDates.Where(d => !used.Contains(d.ServiceCalendarId)).ToList());
    }

    /// <summary>Dani kad kalendar vozi: dani u tjednu iz razdoblja i iznimke.</summary>
    private static IEnumerable<DateOnly> ServiceDates(IServiceCalendarModel calendar, IReadOnlyList<ICalendarDateModel> exceptions)
    {
        var dates = new HashSet<DateOnly>();
        for (var date = calendar.StartDate; date <= calendar.EndDate; date = date.AddDays(1))
        {
            if (ServiceCalendarRules.RunsOnWeekday(calendar, date.DayOfWeek))
            {
                dates.Add(date);
            }
        }

        foreach (var exception in exceptions)
        {
            if (exception.ExceptionType == CalendarExceptionType.Added)
            {
                dates.Add(exception.Date);
            }
            else
            {
                dates.Remove(exception.Date);
            }
        }

        return dates;
    }

    private static ServiceCalendarModel DayType(
        IServiceCalendarModel calendar,
        ITimetableSeasonModel season,
        IReadOnlyDictionary<DayOfWeek, IServiceCalendarModel> regular)
    {
        bool Owns(DayOfWeek day) => regular.GetValueOrDefault(day)?.Id == calendar.Id;

        var dayType = new ServiceCalendarModel
        {
            Id = calendar.Id,
            Code = calendar.Code,
            Name = calendar.Name,
            Monday = Owns(DayOfWeek.Monday),
            Tuesday = Owns(DayOfWeek.Tuesday),
            Wednesday = Owns(DayOfWeek.Wednesday),
            Thursday = Owns(DayOfWeek.Thursday),
            Friday = Owns(DayOfWeek.Friday),
            Saturday = Owns(DayOfWeek.Saturday),
            Sunday = Owns(DayOfWeek.Sunday),
            StartDate = season.StartDate,
            EndDate = season.EndDate,
            SeasonId = season.Id
        };

        // Nazivi prema danima u tjednu: "Radni dan (0_1)" ovdje postaje "Radni dan".
        bool[] weekdays = [dayType.Monday, dayType.Tuesday, dayType.Wednesday, dayType.Thursday, dayType.Friday, dayType.Saturday, dayType.Sunday];
        if (weekdays.Any(d => d))
        {
            dayType.Name = GtfsFeedReader.WeekdayName(weekdays);
        }

        return dayType;
    }

    /// <summary>Dani koji odstupaju od dana u tjednu; "normalno" se ne pamti.</summary>
    private static List<SeasonDay> Days(
        DateOnly start,
        DateOnly end,
        IReadOnlyDictionary<DateOnly, IServiceCalendarModel> running,
        IReadOnlyDictionary<DayOfWeek, IServiceCalendarModel> regular,
        IReadOnlyList<ServiceCalendarModel> dayTypes)
    {
        // Sezona traži različite nazive, a svi dani u tjednu mogu dati isti.
        foreach (var dayType in dayTypes.GroupBy(d => d.Name, StringComparer.CurrentCultureIgnoreCase).Where(g => g.Count() > 1).SelectMany(g => g))
        {
            dayType.Name = $"{dayType.Name} ({dayType.Code})";
        }

        var days = new List<SeasonDay>();
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            var runs = running.GetValueOrDefault(date);
            var expected = regular.GetValueOrDefault(date.DayOfWeek);
            if (runs?.Id == expected?.Id)
            {
                continue;
            }

            days.Add(runs is null
                ? new SeasonDay(date, SeasonDayMode.NoService, null, SeasonDaySource.Manual, null)
                : new SeasonDay(date, SeasonDayMode.RunsAs, runs.Id, SeasonDaySource.Manual, null));
        }

        return days;
    }

    private static string Format(DateOnly date) => date.ToString("d. M. yyyy.", CultureInfo.InvariantCulture);
}

/// <param name="Problem">Zašto se sezona ne može napraviti; null ako može.</param>
/// <param name="OtherCalendars">Kalendari bez vožnji; ostaju izvan sezone.</param>
/// <param name="OtherCalendarDates">Iznimke tih kalendara.</param>
internal sealed record FeedSeason(
    string? Problem,
    ITimetableSeasonModel? Season,
    IReadOnlyList<IServiceCalendarModel> DayTypes,
    IReadOnlyList<ICalendarDateModel> CalendarDates,
    IReadOnlyList<SeasonDay> Days,
    IReadOnlyList<IServiceCalendarModel> OtherCalendars,
    IReadOnlyList<ICalendarDateModel> OtherCalendarDates)
{
    public static FeedSeason Impossible(string problem) => new(problem, null, [], [], [], [], []);
}
