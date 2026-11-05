using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Service;
using Terminus.Service.Gtfs.Import;

namespace Terminus.Tests.Service;

public class FeedSeasonBuilderTests
{
    // 2. 11. 2026. je ponedjeljak.
    private static readonly DateOnly Monday = new(2026, 11, 2);

    private readonly List<IServiceCalendarModel> _calendars = [];
    private readonly List<ICalendarDateModel> _dates = [];
    private readonly List<ITripModel> _trips = [];

    /// <summary>Kalendar sa samo datumima (kao ZET) i jednom vožnjom.</summary>
    private ServiceCalendarModel DatesOnly(string code, string name, params DateOnly[] dates)
    {
        var calendar = Add(new ServiceCalendarModel
        {
            Code = code,
            Name = name,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2030, 12, 31)
        });
        _dates.AddRange(dates.Select(d => new CalendarDateModel { ServiceCalendarId = calendar.Id, Date = d, ExceptionType = CalendarExceptionType.Added }));
        return calendar;
    }

    private ServiceCalendarModel Add(ServiceCalendarModel calendar, bool withTrip = true)
    {
        calendar.Id = Guid.NewGuid();
        _calendars.Add(calendar);
        if (withTrip)
        {
            _trips.Add(new TripModel { Id = Guid.NewGuid(), ServiceCalendarId = calendar.Id });
        }
        return calendar;
    }

    private GtfsFeedData Feed() => new()
    {
        Agencies = [],
        Stops = [],
        Routes = [],
        Calendars = _calendars,
        CalendarDates = _dates,
        Shapes = [],
        Trips = _trips,
        StopTimes = [],
        Fares = []
    };

    private static DateOnly[] Days(params int[] offsets) => offsets.Select(Monday.AddDays).ToArray();

    [Fact]
    public void DatesOnlyFeed_GetsWeekdaysFromMostCommonCalendar()
    {
        var workday = DatesOnly("RD", "Posebni datumi", Days(0, 1, 2, 3, 4, 7, 8, 9, 10, 11));
        var saturday = DatesOnly("SUB", "Posebni datumi: x", Days(5, 12));
        var special = DatesOnly("POS", "Posebni datumi: y", Days(14));

        var result = FeedSeasonBuilder.Build(Feed(), null);

        Assert.Null(result.Problem);
        var season = result.Season!;
        Assert.Equal((Monday, Monday.AddDays(14)), (season.StartDate, season.EndDate));

        var main = result.DayTypes.Single(d => d.Id == workday.Id);
        Assert.Equal(("Radni dan", true, true, false), (main.Name, main.Monday, main.Friday, main.Saturday));
        Assert.Equal(season.Id, main.SeasonId);
        Assert.Equal((season.StartDate, season.EndDate), (main.StartDate, main.EndDate));
        Assert.Equal("Subota", result.DayTypes.Single(d => d.Id == saturday.Id).Name);

        // Poseban kalendar nema dana u tjednu, a ponedjeljak 16. 11. nije običan.
        var other = result.DayTypes.Single(d => d.Id == special.Id);
        Assert.False(other.Monday);
        Assert.Equal("Posebni datumi: y", other.Name);
        var day = Assert.Single(result.Days, d => d.Date == Monday.AddDays(14));
        Assert.Equal((SeasonDayMode.RunsAs, special.Id, SeasonDaySource.Manual), (day.Mode, day.CalendarId!.Value, day.Source));
    }

    [Fact]
    public void Season_RunsExactlyLikeTheFeed()
    {
        var workday = DatesOnly("RD", "R", Days(0, 1, 2, 3, 4, 7, 9, 10, 11));
        var sunday = DatesOnly("NED", "N", Days(6, 13, 8));
        var special = DatesOnly("POS", "P", Days(14));
        var feed = Feed();

        var result = FeedSeasonBuilder.Build(feed, null);

        Assert.Null(result.Problem);
        for (var date = result.Season!.StartDate; date <= result.Season.EndDate; date = date.AddDays(1))
        {
            var before = new[] { workday, sunday, special }.Where(c => feed.CalendarDates.Any(d => d.ServiceCalendarId == c.Id && d.Date == date)).Select(c => c.Id);
            var after = ServiceCalendarRules.ActiveOn(result.DayTypes, result.CalendarDates, date);
            Assert.Equal(before.Order(), after.Order());
        }
    }

    [Fact]
    public void DayWithoutService_InsideSeason_IsNoService()
    {
        // Vozi ponedjeljkom, ali ne i srednjeg ponedjeljka.
        DatesOnly("RD", "R", Days(0, 14));

        var result = FeedSeasonBuilder.Build(Feed(), null);

        var day = Assert.Single(result.Days);
        Assert.Equal((Monday.AddDays(7), SeasonDayMode.NoService, (Guid?)null), (day.Date, day.Mode, day.CalendarId));
    }

    [Fact]
    public void TwoCalendarsOnTheSameDay_CannotBecomeSeason()
    {
        DatesOnly("A", "A", Days(0));
        DatesOnly("B", "B", Days(0));

        var result = FeedSeasonBuilder.Build(Feed(), null);

        Assert.Contains("više kalendara", result.Problem);
        Assert.Null(result.Season);
    }

    [Fact]
    public void WeeklyCalendars_KeepTheirWeekdaysAndExceptions()
    {
        var weekly = Add(new ServiceCalendarModel
        {
            Code = "RD",
            Name = "Radni dan",
            Monday = true,
            Tuesday = true,
            Wednesday = true,
            Thursday = true,
            Friday = true,
            StartDate = Monday,
            EndDate = Monday.AddDays(13)
        });
        _dates.Add(new CalendarDateModel { ServiceCalendarId = weekly.Id, Date = Monday.AddDays(2), ExceptionType = CalendarExceptionType.Removed });

        var result = FeedSeasonBuilder.Build(Feed(), null);

        var dayType = Assert.Single(result.DayTypes);
        Assert.True(dayType.Monday);
        var removed = Assert.Single(result.CalendarDates);
        Assert.Equal((Monday.AddDays(2), CalendarExceptionType.Removed), (removed.Date, removed.ExceptionType));
    }

    [Fact]
    public void From_CutsOffEarlierDays()
    {
        var oldSaturday = DatesOnly("OLD", "O", Days(5));
        DatesOnly("RD", "R", Days(0, 7, 8));

        var result = FeedSeasonBuilder.Build(Feed(), Monday.AddDays(7));

        Assert.Equal(Monday.AddDays(7), result.Season!.StartDate);
        Assert.Empty(result.Days);
        Assert.Equal(oldSaturday.Id, result.DayTypes.Single(d => d.Code == "OLD").Id);
    }

    [Fact]
    public void CalendarWithoutTrips_StaysOutsideTheSeason()
    {
        DatesOnly("RD", "R", Days(0));
        var unused = Add(new ServiceCalendarModel { Code = "X", Name = "X", Monday = true, StartDate = Monday, EndDate = Monday }, withTrip: false);

        var result = FeedSeasonBuilder.Build(Feed(), null);

        Assert.Null(result.Problem);
        Assert.Equal("RD", Assert.Single(result.DayTypes).Code);
        Assert.Equal(unused.Id, Assert.Single(result.OtherCalendars).Id);
    }

    [Fact]
    public void TooManyCalendars_CannotBecomeSeason()
    {
        for (var i = 0; i <= SeasonService.MaxDayTypes; i++)
        {
            DatesOnly($"C{i}", $"C{i}", Days(i));
        }

        Assert.Contains("tipova dana", FeedSeasonBuilder.Build(Feed(), null).Problem);
    }

    [Fact]
    public void TooLongFeed_CannotBecomeSeason()
    {
        DatesOnly("RD", "R", Monday, Monday.AddDays(SeasonService.MaxDays));

        Assert.Contains("dulje", FeedSeasonBuilder.Build(Feed(), null).Problem);
    }

    [Fact]
    public void FeedWithoutTrips_CannotBecomeSeason()
    {
        Assert.NotNull(FeedSeasonBuilder.Build(Feed(), null).Problem);
    }

    [Fact]
    public void SameNames_GetCodes()
    {
        var first = Add(new ServiceCalendarModel { Code = "A", Name = "Radni dan", Monday = true, StartDate = Monday, EndDate = Monday.AddDays(13) });
        var second = DatesOnly("B", "Radni dan", Monday.AddDays(1));

        var result = FeedSeasonBuilder.Build(Feed(), null);

        var names = result.DayTypes.Select(d => d.Name).Order().ToList();
        Assert.Equal(names.Distinct().Count(), names.Count);
        Assert.Contains(result.DayTypes, d => d.Id == first.Id);
        Assert.Contains(result.DayTypes, d => d.Id == second.Id);
    }
}
