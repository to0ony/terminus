using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Service.Gtfs.Import;

namespace Terminus.Tests.Service;

public class ExpiredCalendarsTests
{
    private static readonly DateOnly Today = new(2026, 11, 5);

    private static ServiceCalendarModel Weekly(DateOnly start, DateOnly end) => new()
    {
        Id = Guid.NewGuid(),
        Code = "C",
        Monday = true,
        StartDate = start,
        EndDate = end
    };

    private static ServiceCalendarModel DatesOnly() => new()
    {
        Id = Guid.NewGuid(),
        Code = "C",
        StartDate = new DateOnly(2026, 10, 21),
        EndDate = new DateOnly(2030, 12, 31)
    };

    private static ICalendarDateModel Date(IServiceCalendarModel calendar, DateOnly date, CalendarExceptionType type) =>
        new CalendarDateModel { ServiceCalendarId = calendar.Id, Date = date, ExceptionType = type };

    private static bool IsExpired(IServiceCalendarModel calendar, params ICalendarDateModel[] dates) =>
        ExpiredCalendars.Find([calendar], dates, Today).Contains(calendar.Id);

    [Fact]
    public void WeeklyCalendar_EndedBeforeToday_IsExpired() =>
        Assert.True(IsExpired(Weekly(new DateOnly(2026, 1, 1), new DateOnly(2026, 11, 4))));

    [Fact]
    public void WeeklyCalendar_WithDayAhead_IsNotExpired() =>
        Assert.False(IsExpired(Weekly(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31))));

    [Fact]
    public void WeeklyCalendar_StartingLater_IsNotExpired() =>
        Assert.False(IsExpired(Weekly(new DateOnly(2027, 1, 1), new DateOnly(2027, 12, 31))));

    [Fact]
    public void WeeklyCalendar_WithoutItsWeekdayInRange_IsExpired() =>
        // Četvrtak 5. 11. do subote 7. 11.: ponedjeljak ne pada u razdoblje.
        Assert.True(IsExpired(Weekly(Today, new DateOnly(2026, 11, 7))));

    [Fact]
    public void WeeklyCalendar_WithRemainingDaysRemoved_IsExpired()
    {
        var calendar = Weekly(new DateOnly(2026, 1, 1), new DateOnly(2026, 11, 16));

        Assert.True(IsExpired(
            calendar,
            Date(calendar, new DateOnly(2026, 11, 9), CalendarExceptionType.Removed),
            Date(calendar, new DateOnly(2026, 11, 16), CalendarExceptionType.Removed)));
    }

    [Fact]
    public void DatesOnlyCalendar_WithPastDatesOnly_IsExpiredDespiteLongPeriod()
    {
        var calendar = DatesOnly();

        Assert.True(IsExpired(calendar, Date(calendar, new DateOnly(2026, 10, 24), CalendarExceptionType.Added)));
    }

    [Fact]
    public void DatesOnlyCalendar_RunningToday_IsNotExpired()
    {
        var calendar = DatesOnly();

        Assert.False(IsExpired(calendar, Date(calendar, Today, CalendarExceptionType.Added)));
    }

    [Fact]
    public void CalendarWithExtraDayAfterPeriod_IsNotExpired()
    {
        var calendar = Weekly(new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30));

        Assert.False(IsExpired(calendar, Date(calendar, new DateOnly(2026, 12, 25), CalendarExceptionType.Added)));
    }
}
