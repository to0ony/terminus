using Terminus.Model.Common;
using Terminus.Service.Common;

namespace Terminus.Service;

/// <summary>
/// Slaže polaske stanice u vozni red za ispis. Ne gleda nazive kalendara, nego
/// koji polasci stvarno voze na koji datum, pa radi i za feedove koji cijeli
/// raspored drže u calendar_dates.txt (svaki tjedan svoji kalendari).
/// </summary>
/// <remarks>
/// Za svaku liniju i smjer:
/// <list type="number">
/// <item>Za svaki dan u tjednu uzima se najčešći skup polazaka među njegovim datumima.</item>
/// <item>Dani u tjednu s istim skupom čine stupac (npr. pon–pet = "radni dan").</item>
/// <item>Datum koji odstupa: bez polazaka postaje "ne vozi", s voznim redom drugog
/// stupca "vozi kao", a inače ide u stupac s kojim dijeli najviše polazaka i
/// razlike se označavaju slovima uz polaske.</item>
/// </list>
/// </remarks>
internal static class StopTimetableBuilder
{
    private static readonly DayOfWeek[] Week =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    ];

    public static IReadOnlyList<TimetableSection> Build(
        IReadOnlyList<ScheduledDeparture> departures,
        IReadOnlyList<IServiceCalendarModel> calendars,
        IReadOnlyCollection<ICalendarDateModel> calendarDates,
        DateOnly from,
        DateOnly to)
    {
        // Mnogo datuma dijeli isti skup kalendara, pa se polasci računaju po skupu.
        var exceptions = calendarDates.ToLookup(d => d.Date);
        var calendarSets = new Dictionary<string, IReadOnlySet<Guid>>(StringComparer.Ordinal);
        var dates = new List<(DateOnly Date, string CalendarSet)>();

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var active = ServiceCalendarRules.ActiveOn(calendars, exceptions[date].ToList(), date);
            var key = string.Join(',', active.Order());
            calendarSets.TryAdd(key, active);
            dates.Add((date, key));
        }

        return departures
            .GroupBy(d => (d.RouteId, d.Headsign, d.PlatformCode))
            .Select(group => BuildSection(group.ToList(), calendarSets, dates))
            .OfType<TimetableSection>()
            .OrderBy(s => (s.RouteShortName ?? s.RouteLongName ?? "").Length)
            .ThenBy(s => s.RouteShortName ?? s.RouteLongName, StringComparer.Ordinal)
            .ThenBy(s => s.Headsign, StringComparer.Ordinal)
            .ThenBy(s => s.Platform, StringComparer.Ordinal)
            .ToList();
    }

    private static TimetableSection? BuildSection(
        IReadOnlyList<ScheduledDeparture> departures,
        Dictionary<string, IReadOnlySet<Guid>> calendarSets,
        IReadOnlyList<(DateOnly Date, string CalendarSet)> dates)
    {
        // Isti skup polazaka dobiva isti broj, pa se dani uspoređuju brojem.
        var sets = new List<int[]>();
        var setIds = new Dictionary<string, int>(StringComparer.Ordinal);
        var setByCalendars = calendarSets.ToDictionary(
            kv => kv.Key,
            kv =>
            {
                var times = departures
                    .Where(d => kv.Value.Contains(d.ServiceCalendarId))
                    .Select(d => d.DepartureTime)
                    .Distinct()
                    .Order()
                    .ToArray();
                var key = string.Join(',', times);
                if (!setIds.TryGetValue(key, out var id))
                {
                    id = sets.Count;
                    sets.Add(times);
                    setIds[key] = id;
                }
                return id;
            },
            StringComparer.Ordinal);

        var days = dates.Select(d => (d.Date, Set: setByCalendars[d.CalendarSet])).ToList();
        if (days.All(d => sets[d.Set].Length == 0))
        {
            return null;
        }

        // Kod izjednačenja (kratko razdoblje) prednost ima skup koji je čest i inače,
        // pa jedan praznik u dva tjedna ne postaje vozni red tog dana u tjednu.
        var overall = days.CountBy(d => d.Set).ToDictionary();
        var typical = days
            .GroupBy(d => d.Date.DayOfWeek)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(d => d.Set)
                    .OrderByDescending(s => s.Count())
                    .ThenByDescending(s => overall[s.Key])
                    .ThenBy(s => s.First().Date)
                    .First().Key);

        var columns = new List<ColumnBuilder>();
        foreach (var weekday in Week)
        {
            if (typical.TryGetValue(weekday, out var set) && sets[set].Length > 0)
            {
                var column = columns.Find(c => c.Set == set);
                if (column is null)
                {
                    column = new ColumnBuilder(set);
                    columns.Add(column);
                }
                column.Weekdays.Add(weekday);
            }
        }

        var weekdayColumns = columns.ToList();
        var noService = new List<DateOnly>();
        var runsAs = new Dictionary<ColumnBuilder, List<DateOnly>>();

        foreach (var (date, set) in days)
        {
            var own = weekdayColumns.Find(c => c.Weekdays.Contains(date.DayOfWeek));
            if (set == typical[date.DayOfWeek])
            {
                own?.Dates.Add(date);
            }
            else if (sets[set].Length == 0)
            {
                noService.Add(date);
            }
            else if (weekdayColumns.Find(c => c.Set == set) is { } same)
            {
                if (!runsAs.TryGetValue(same, out var list))
                {
                    runsAs[same] = list = [];
                }
                list.Add(date);
            }
            else
            {
                var best = weekdayColumns
                    .Select(c => (Column: c, Overlap: sets[c.Set].Intersect(sets[set]).Count()))
                    .Where(x => x.Overlap > 0)
                    .OrderByDescending(x => x.Overlap)
                    .ThenByDescending(x => x.Column == own)
                    .Select(x => x.Column)
                    .FirstOrDefault();

                if (best is null)
                {
                    // Polasci samo na posebne datume, bez sličnog dana u tjednu.
                    best = columns.Find(c => c.Weekdays.Count == 0 && c.Set == set);
                    if (best is null)
                    {
                        best = new ColumnBuilder(set);
                        columns.Add(best);
                    }
                }
                best.Dates.Add(date);
            }
        }

        var daySets = days.ToDictionary(d => d.Date, d => sets[d.Set]);
        var marks = new Dictionary<(bool RunsOnly, string Dates), TimetableFootnote>();

        var built = columns
            .Select(column => new TimetableColumn(
                column.Weekdays,
                column.Weekdays.Count == 0 ? column.Dates : [],
                BuildTimes(column, daySets, marks)))
            .ToList();

        var first = departures[0];
        return new TimetableSection(
            first.RouteShortName,
            first.RouteLongName,
            first.RouteColor,
            first.RouteTextColor,
            first.Headsign,
            first.PlatformCode,
            built,
            marks.Values.ToList(),
            runsAs.Select(kv => new TimetableRunsAs(columns.IndexOf(kv.Key), kv.Value)).OrderBy(r => r.ColumnIndex).ToList(),
            noService);
    }

    /// <summary>
    /// Polasci stupca sa svih njegovih datuma. Polazak koji ne vozi na sve datume
    /// dobiva slovo: "ne vozi …" ili, ako vozi na manje od pola datuma, "vozi samo …".
    /// </summary>
    private static List<TimetableTime> BuildTimes(
        ColumnBuilder column,
        Dictionary<DateOnly, int[]> daySets,
        Dictionary<(bool RunsOnly, string Dates), TimetableFootnote> marks)
    {
        var times = column.Dates.SelectMany(d => daySets[d]).Distinct().Order();
        var result = new List<TimetableTime>();

        foreach (var seconds in times)
        {
            var runsOn = column.Dates.Where(d => Array.BinarySearch(daySets[d], seconds) >= 0).ToList();
            if (runsOn.Count == column.Dates.Count)
            {
                result.Add(new TimetableTime(seconds, null));
                continue;
            }

            var notOn = column.Dates.Except(runsOn).ToList();
            var runsOnly = runsOn.Count < notOn.Count;
            var noteDates = runsOnly ? runsOn : notOn;
            var key = (runsOnly, string.Join(',', noteDates));

            if (!marks.TryGetValue(key, out var footnote))
            {
                footnote = new TimetableFootnote(Mark(marks.Count), runsOnly, noteDates);
                marks[key] = footnote;
            }
            result.Add(new TimetableTime(seconds, footnote.Mark));
        }

        return result;
    }

    /// <summary>a–z, zatim a2–z2 itd.</summary>
    private static string Mark(int index)
    {
        var letter = (char)('a' + index % 26);
        return index < 26 ? letter.ToString() : $"{letter}{index / 26 + 1}";
    }

    private sealed class ColumnBuilder(int set)
    {
        public int Set { get; } = set;
        public List<DayOfWeek> Weekdays { get; } = [];
        public List<DateOnly> Dates { get; } = [];
    }
}
