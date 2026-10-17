using Terminus.Common;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Service;
using Terminus.Service.Common;

namespace Terminus.Tests.Service;

public class StopTimetableBuilderTests
{
    // Ponedjeljak 7.12. do nedjelje 20.12.2026.: dva puna tjedna.
    private static readonly DateOnly From = new(2026, 12, 7);
    private static readonly DateOnly To = new(2026, 12, 20);

    private static readonly DayOfWeek[] Workdays =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];

    private readonly Guid _route = Guid.NewGuid();
    private readonly List<ServiceCalendarModel> _calendars = [];
    private readonly List<CalendarDateModel> _dates = [];
    private readonly List<ScheduledDeparture> _departures = [];

    [Fact]
    public void Build_GroupsWeekdaysWithTheSameTimetableIntoColumns()
    {
        var workdays = Calendar(Workdays);
        var saturday = Calendar([DayOfWeek.Saturday]);
        var sunday = Calendar([DayOfWeek.Sunday]);
        Departure(workdays, "06:15:00", "07:15:00");
        Departure(saturday, "08:00:00");
        Departure(sunday, "09:30:00");

        var section = Assert.Single(Build());

        Assert.Equal(3, section.Columns.Count);
        Assert.Equal(Workdays, section.Columns[0].Weekdays);
        Assert.Equal([DayOfWeek.Saturday], section.Columns[1].Weekdays);
        Assert.Equal([DayOfWeek.Sunday], section.Columns[2].Weekdays);
        Assert.Equal(["06:15:00", "07:15:00"], Times(section.Columns[0]));
        Assert.All(section.Columns.SelectMany(c => c.Times), t => Assert.Null(t.Mark));
        Assert.Empty(section.Footnotes);
        Assert.Empty(section.RunsAs);
        Assert.Empty(section.NoServiceDates);
    }

    [Fact]
    public void Build_SameTimesEveryDay_IsOneColumn()
    {
        var daily = Calendar([.. Enum.GetValues<DayOfWeek>()]);
        Departure(daily, "10:00:00");

        var column = Assert.Single(Assert.Single(Build()).Columns);

        Assert.Equal(7, column.Weekdays.Count);
        Assert.Equal(DayOfWeek.Monday, column.Weekdays[0]);
    }

    [Fact]
    public void Build_HolidayWithSundayTimetable_IsRunsAs()
    {
        var workdays = Calendar(Workdays);
        var sunday = Calendar([DayOfWeek.Sunday]);
        Departure(workdays, "06:15:00");
        Departure(sunday, "09:30:00");

        // Srijeda 9.12. vozi kao nedjelja.
        var holiday = new DateOnly(2026, 12, 9);
        Exception(workdays, holiday, CalendarExceptionType.Removed);
        Exception(sunday, holiday, CalendarExceptionType.Added);

        var section = Assert.Single(Build());

        var runsAs = Assert.Single(section.RunsAs);
        Assert.Equal(1, runsAs.ColumnIndex);
        Assert.Equal([holiday], runsAs.Dates);
        Assert.Empty(section.Footnotes);
    }

    [Fact]
    public void Build_DayWithoutService_IsNoServiceDate()
    {
        var workdays = Calendar(Workdays);
        Departure(workdays, "06:15:00");
        var holiday = new DateOnly(2026, 12, 9);
        Exception(workdays, holiday, CalendarExceptionType.Removed);

        var section = Assert.Single(Build());

        Assert.Equal([holiday], section.NoServiceDates);
        Assert.All(section.Columns[0].Times, t => Assert.Null(t.Mark));
    }

    [Fact]
    public void Build_DepartureMissingOnSomeDates_IsMarked()
    {
        var workdays = Calendar(Workdays);
        var school = Calendar(Workdays);
        Departure(workdays, "06:15:00");
        Departure(school, "07:05:00");
        Exception(school, new DateOnly(2026, 12, 10), CalendarExceptionType.Removed);
        Exception(school, new DateOnly(2026, 12, 15), CalendarExceptionType.Removed);

        var section = Assert.Single(Build());
        var column = Assert.Single(section.Columns);

        Assert.Equal([null, "a"], column.Times.Select(t => t.Mark));
        var footnote = Assert.Single(section.Footnotes);
        Assert.False(footnote.RunsOnly);
        Assert.Equal([new DateOnly(2026, 12, 10), new DateOnly(2026, 12, 15)], footnote.Dates);
    }

    [Fact]
    public void Build_ExtraDepartureOnOneDate_IsMarkedRunsOnly()
    {
        var workdays = Calendar(Workdays);
        var extra = Calendar([]);
        Departure(workdays, "06:15:00");
        Departure(extra, "22:00:00");
        Exception(extra, new DateOnly(2026, 12, 11), CalendarExceptionType.Added);

        var section = Assert.Single(Build());
        var column = Assert.Single(section.Columns);

        Assert.Equal(["06:15:00", "22:00:00"], Times(column));
        var footnote = Assert.Single(section.Footnotes);
        Assert.True(footnote.RunsOnly);
        Assert.Equal([new DateOnly(2026, 12, 11)], footnote.Dates);
        Assert.Equal("a", column.Times[1].Mark);
    }

    [Fact]
    public void Build_WeeklyCalendarsInCalendarDates_MergeIntoOneTimetable()
    {
        // Kao ZET: svaki tjedan svoj kalendar, samo s dodanim datumima.
        var week1 = Calendar([]);
        var week2 = Calendar([]);
        Departure(week1, "05:00:00", "24:10:00");
        Departure(week2, "05:00:00", "24:10:00");
        for (var date = From; date <= To; date = date.AddDays(1))
        {
            if (date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                Exception(date.Day < 14 ? week1 : week2, date, CalendarExceptionType.Added);
            }
        }

        var section = Assert.Single(Build());
        var column = Assert.Single(section.Columns);

        Assert.Equal(Workdays, column.Weekdays);
        Assert.Equal(["05:00:00", "24:10:00"], Times(column));
        Assert.Empty(section.Footnotes);
    }

    [Fact]
    public void Build_OnlySpecialDates_IsSpecialColumn()
    {
        var special = Calendar([]);
        Departure(special, "12:00:00");
        Exception(special, new DateOnly(2026, 12, 12), CalendarExceptionType.Added);

        var column = Assert.Single(Assert.Single(Build()).Columns);

        Assert.Empty(column.Weekdays);
        Assert.Equal([new DateOnly(2026, 12, 12)], column.SpecialDates);
    }

    [Fact]
    public void Build_SplitsByRouteAndHeadsign_InRouteOrder()
    {
        var daily = Calendar([.. Enum.GetValues<DayOfWeek>()]);
        var route11 = Guid.NewGuid();
        var route2 = Guid.NewGuid();
        Departure(daily, route11, "11", "Dubec", "06:00:00");
        Departure(daily, route2, "2", "Savski most", "06:00:00");
        Departure(daily, route2, "2", "Črnomerec", "06:30:00");

        var sections = Build();

        Assert.Equal(
            [("2", "Savski most"), ("2", "Črnomerec"), ("11", "Dubec")],
            sections.Select(s => (s.RouteShortName!, s.Headsign)));
    }

    [Fact]
    public void Build_NoDeparturesInPeriod_ReturnsNoSections()
    {
        var past = Calendar(Workdays);
        past.EndDate = From.AddDays(-1);
        Departure(past, "06:15:00");

        Assert.Empty(Build());
    }

    private IReadOnlyList<TimetableSection> Build() =>
        StopTimetableBuilder.Build(_departures, _calendars, _dates, From, To);

    private static IEnumerable<string> Times(TimetableColumn column) =>
        column.Times.Select(t => GtfsTime.Format(t.Seconds));

    private ServiceCalendarModel Calendar(DayOfWeek[] days)
    {
        var calendar = new ServiceCalendarModel
        {
            Id = Guid.NewGuid(),
            Monday = days.Contains(DayOfWeek.Monday),
            Tuesday = days.Contains(DayOfWeek.Tuesday),
            Wednesday = days.Contains(DayOfWeek.Wednesday),
            Thursday = days.Contains(DayOfWeek.Thursday),
            Friday = days.Contains(DayOfWeek.Friday),
            Saturday = days.Contains(DayOfWeek.Saturday),
            Sunday = days.Contains(DayOfWeek.Sunday),
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31)
        };
        _calendars.Add(calendar);
        return calendar;
    }

    private void Exception(ServiceCalendarModel calendar, DateOnly date, CalendarExceptionType type) =>
        _dates.Add(new CalendarDateModel { ServiceCalendarId = calendar.Id, Date = date, ExceptionType = type });

    private void Departure(ServiceCalendarModel calendar, params string[] times) =>
        Departure(calendar, _route, "6", "Sopot", times);

    private void Departure(ServiceCalendarModel calendar, Guid route, string routeName, string headsign, params string[] times)
    {
        foreach (var time in times)
        {
            Assert.True(GtfsTime.TryParse(time, out var seconds));
            _departures.Add(new ScheduledDeparture(
                Guid.NewGuid(), calendar.Id, route, routeName, null, null, null, headsign, Guid.Empty, null, seconds));
        }
    }
}
