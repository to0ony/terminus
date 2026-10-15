using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Service;

namespace Terminus.Tests.Service;

/// <summary>Primjeri iz docs/departures-and-timetables.md, poglavlje 1.1.</summary>
public class ServiceCalendarRulesTests
{
    /// <summary>"Radni dan", pon–pet, 1.1.–31.12.2026., ne vozi 25.12.</summary>
    private static readonly ServiceCalendarModel Workdays = new()
    {
        Id = Guid.NewGuid(),
        Code = "RD",
        Name = "Radni dan",
        Monday = true,
        Tuesday = true,
        Wednesday = true,
        Thursday = true,
        Friday = true,
        StartDate = new DateOnly(2026, 1, 1),
        EndDate = new DateOnly(2026, 12, 31)
    };

    private static readonly ICalendarDateModel[] Christmas =
    [
        Exception(Workdays, new DateOnly(2026, 12, 25), CalendarExceptionType.Removed)
    ];

    [Fact]
    public void RunsOn_WorkdayInPeriod_ReturnsTrue()
    {
        Assert.True(ServiceCalendarRules.RunsOn(Workdays, Christmas, new DateOnly(2026, 12, 23)));
    }

    [Fact]
    public void RunsOn_RemovedDate_ReturnsFalse()
    {
        Assert.False(ServiceCalendarRules.RunsOn(Workdays, Christmas, new DateOnly(2026, 12, 25)));
    }

    [Fact]
    public void RunsOn_UnmarkedWeekday_ReturnsFalse()
    {
        Assert.False(ServiceCalendarRules.RunsOn(Workdays, Christmas, new DateOnly(2026, 12, 26)));
    }

    [Fact]
    public void RunsOn_OutsidePeriod_ReturnsFalse()
    {
        // Ponedjeljak 4.1.2027. je radni dan, ali izvan razdoblja kalendara.
        Assert.False(ServiceCalendarRules.RunsOn(Workdays, Christmas, new DateOnly(2027, 1, 4)));
    }

    [Fact]
    public void RunsOn_AddedDateOutsidePeriodOnUnmarkedDay_ReturnsTrue()
    {
        ICalendarDateModel[] exceptions = [Exception(Workdays, new DateOnly(2027, 1, 2), CalendarExceptionType.Added)];

        Assert.True(ServiceCalendarRules.RunsOn(Workdays, exceptions, new DateOnly(2027, 1, 2)));
    }

    [Fact]
    public void RunsOn_ExceptionOfAnotherCalendar_IsIgnored()
    {
        var other = new ServiceCalendarModel { Id = Guid.NewGuid() };
        ICalendarDateModel[] exceptions = [Exception(other, new DateOnly(2026, 12, 23), CalendarExceptionType.Removed)];

        Assert.True(ServiceCalendarRules.RunsOn(Workdays, exceptions, new DateOnly(2026, 12, 23)));
    }

    [Fact]
    public void RunsOn_PeriodBoundaries_AreInclusive()
    {
        // 1.1.2026. je četvrtak, 31.12.2026. četvrtak.
        Assert.True(ServiceCalendarRules.RunsOn(Workdays, [], new DateOnly(2026, 1, 1)));
        Assert.True(ServiceCalendarRules.RunsOn(Workdays, [], new DateOnly(2026, 12, 31)));
    }

    [Fact]
    public void ActiveOn_ReturnsOnlyCalendarsRunningThatDay()
    {
        var saturday = new ServiceCalendarModel
        {
            Id = Guid.NewGuid(),
            Saturday = true,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31)
        };

        var active = ServiceCalendarRules.ActiveOn([Workdays, saturday], Christmas, new DateOnly(2026, 12, 26));

        Assert.Equal([saturday.Id], active);
    }

    private static CalendarDateModel Exception(IServiceCalendarModel calendar, DateOnly date, CalendarExceptionType type) => new()
    {
        ServiceCalendarId = calendar.Id,
        Date = date,
        ExceptionType = type
    };
}
