using System.Globalization;
using Terminus.Service.Common;

namespace Terminus.Service.Pdf;

/// <summary>Tekstovi PDF voznog reda na hrvatskom i engleskom.</summary>
internal abstract class TimetableTexts
{
    public static TimetableTexts For(TimetableLanguage language) => language switch
    {
        TimetableLanguage.En => new EnglishTexts(),
        _ => new CroatianTexts()
    };

    public abstract string FilePrefix { get; }
    public abstract string Stop { get; }
    public abstract string Hour { get; }
    public abstract string LiveDepartures { get; }
    public abstract string ScheduledTimes { get; }
    public abstract string Page { get; }

    public abstract string Period(DateOnly from, DateOnly to);
    public abstract string Direction(string headsign);
    public abstract string Platform(string platform);
    /// <param name="named">Datumi počinju nazivom (npr. razdoblja), pa ih uvodi dvotočka.</param>
    public abstract string NotOn(string mark, string dates, bool named);
    /// <param name="named">Datumi počinju nazivom (npr. razdoblja), pa ih uvodi dvotočka.</param>
    public abstract string OnlyOn(string mark, string dates, bool named);
    public abstract string RunsAs(string dates, string column);
    /// <param name="named">Datumi počinju nazivom (npr. razdoblja), pa ih uvodi dvotočka.</param>
    public abstract string NoService(string dates, bool named);

    /// <summary>Puni i kratki nazivi dana, od ponedjeljka.</summary>
    protected abstract string[] Days { get; }
    protected abstract string[] ShortDays { get; }
    protected abstract string Daily { get; }
    protected abstract string Workdays { get; }
    protected abstract string WorkdaysAndSaturday { get; }
    protected abstract string Weekend { get; }
    protected abstract string Only(string dates);
    protected abstract string And { get; }
    protected abstract string Date(DateOnly date, bool withYear);

    /// <summary>Naslov stupca, npr. "Radni dan", "Subota", "Pon–Čet" ili "Samo 24. 12.".</summary>
    public string Column(TimetableColumn column, bool withYear)
    {
        if (column.Weekdays.Count == 0)
        {
            return Only(Dates(column.SpecialDates, withYear));
        }

        var days = Enumerable.Range(0, 7).Select(i => column.Weekdays.Contains(Weekday(i))).ToArray();
        var count = days.Count(d => d);
        var workdays = days[..5].All(d => d);

        return count switch
        {
            7 => Daily,
            1 => Days[Array.IndexOf(days, true)],
            5 when workdays => Workdays,
            6 when workdays && days[5] => WorkdaysAndSaturday,
            2 when days[5] && days[6] => Weekend,
            _ => DayRanges(days)
        };
    }

    /// <summary>
    /// Nazivi s rasponom u zagradi, pa ostali rasponi: "Ljetni raspust (15. 6. – 4. 9.),
    /// Božić (25. 12.) i 2. 1.".
    /// </summary>
    public string Dates(TimetableDates dates, bool withYear)
    {
        var parts = dates.Named
            .Select(n => $"{n.Name} ({Join(n.Ranges.SelectMany(r => Range(r, withYear)).ToList())})")
            .Concat(dates.Ranges.SelectMany(r => Range(r, withYear)))
            .ToList();
        return Join(parts);
    }

    /// <summary>Raspon; dva uzastopna dana kao dva datuma, jedan dan kao datum.</summary>
    private IEnumerable<string> Range(DateRange range, bool withYear)
    {
        var days = range.To.DayNumber - range.From.DayNumber;
        if (days == 0)
        {
            return [Date(range.From, withYear)];
        }
        return days == 1
            ? [Date(range.From, withYear), Date(range.To, withYear)]
            : [$"{Date(range.From, withYear)} – {Date(range.To, withYear)}"];
    }

    private string Join(IReadOnlyList<string> parts) =>
        parts.Count <= 1
            ? string.Concat(parts)
            : $"{string.Join(", ", parts.Take(parts.Count - 1))} {And} {parts[^1]}";

    /// <summary>Datumi s nizovima uzastopnih dana kao rasponom: "24. 12. – 27. 12., 31. 12. i 1. 1.".</summary>
    public string Dates(IReadOnlyList<DateOnly> dates, bool withYear)
    {
        var parts = new List<string>();
        for (var i = 0; i < dates.Count;)
        {
            var end = i;
            while (end + 1 < dates.Count && dates[end + 1] == dates[end].AddDays(1))
            {
                end++;
            }

            parts.Add(end - i >= 2
                ? $"{Date(dates[i], withYear)} – {Date(dates[end], withYear)}"
                : Date(dates[i], withYear));
            if (end - i == 1)
            {
                parts.Add(Date(dates[end], withYear));
            }
            i = end + 1;
        }

        return parts.Count <= 1
            ? string.Concat(parts)
            : $"{string.Join(", ", parts[..^1])} {And} {parts[^1]}";
    }

    /// <summary>Uzastopni dani kao raspon: "Pon–Čet, Sub".</summary>
    private string DayRanges(bool[] days)
    {
        var parts = new List<string>();
        for (var i = 0; i < 7; i++)
        {
            if (!days[i])
            {
                continue;
            }
            var end = i;
            while (end + 1 < 7 && days[end + 1])
            {
                end++;
            }
            parts.Add(end > i ? $"{ShortDays[i]}–{ShortDays[end]}" : ShortDays[i]);
            i = end;
        }
        return string.Join(", ", parts);
    }

    private static DayOfWeek Weekday(int mondayBased) => (DayOfWeek)((mondayBased + 1) % 7);
}

internal sealed class CroatianTexts : TimetableTexts
{
    public override string FilePrefix => "vozni-red";
    public override string Stop => "Stanica";
    public override string Hour => "sat";
    public override string LiveDepartures => "Polasci uživo";
    public override string ScheduledTimes => "Vremena su prema voznom redu.";

    public override string Period(DateOnly from, DateOnly to) => from == to
        ? $"Vrijedi {Date(from, true)}"
        : $"Vrijedi od {Date(from, true)} do {Date(to, true)}";

    public override string Direction(string headsign) => $"smjer {headsign}";
    public override string Platform(string platform) => $"peron {platform}";
    public override string Page => "Stranica";
    public override string NotOn(string mark, string dates, bool named) => $"{mark} – ne vozi{(named ? ":" : "")} {dates}";
    public override string OnlyOn(string mark, string dates, bool named) => $"{mark} – vozi samo{(named ? ":" : "")} {dates}";
    // Datum već završava točkom ("25. 12.").
    public override string RunsAs(string dates, string column) => $"{dates} vozi kao „{column}”.";
    public override string NoService(string dates, bool named) => $"Ne vozi{(named ? ":" : "")} {dates}";

    protected override string[] Days => ["Ponedjeljak", "Utorak", "Srijeda", "Četvrtak", "Petak", "Subota", "Nedjelja"];
    protected override string[] ShortDays => ["Pon", "Uto", "Sri", "Čet", "Pet", "Sub", "Ned"];
    protected override string Daily => "Svaki dan";
    protected override string Workdays => "Radni dan";
    protected override string WorkdaysAndSaturday => "Radni dan i subota";
    protected override string Weekend => "Subota i nedjelja";
    protected override string And => "i";
    protected override string Only(string dates) => $"Samo {dates}";

    protected override string Date(DateOnly date, bool withYear) => withYear
        ? $"{date.Day}. {date.Month}. {date.Year}."
        : $"{date.Day}. {date.Month}.";
}

internal sealed class EnglishTexts : TimetableTexts
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public override string FilePrefix => "timetable";
    public override string Stop => "Stop";
    public override string Hour => "hour";
    public override string LiveDepartures => "Live departures";
    public override string ScheduledTimes => "Times are as scheduled.";

    public override string Period(DateOnly from, DateOnly to) => from == to
        ? $"Valid on {Date(from, true)}"
        : $"Valid from {Date(from, true)} to {Date(to, true)}";

    public override string Direction(string headsign) => $"to {headsign}";
    public override string Platform(string platform) => $"platform {platform}";
    public override string Page => "Page";
    public override string NotOn(string mark, string dates, bool named) => $"{mark} – not on{(named ? ":" : "")} {dates}";
    public override string OnlyOn(string mark, string dates, bool named) => $"{mark} – only on{(named ? ":" : "")} {dates}";
    public override string RunsAs(string dates, string column) => $"On {dates}: {column} timetable.";
    public override string NoService(string dates, bool named) => $"No service on{(named ? ":" : "")} {dates}.";

    protected override string[] Days => ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];
    protected override string[] ShortDays => ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
    protected override string Daily => "Daily";
    protected override string Workdays => "Monday–Friday";
    protected override string WorkdaysAndSaturday => "Monday–Saturday";
    protected override string Weekend => "Weekends";
    protected override string And => "and";
    protected override string Only(string dates) => $"Only {dates}";

    protected override string Date(DateOnly date, bool withYear) =>
        date.ToString(withYear ? "d MMM yyyy" : "d MMM", Culture);
}
