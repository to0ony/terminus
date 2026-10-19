using Terminus.Common.Exceptions;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Service;
using Terminus.Service.Common;
using Terminus.Tests.Fakes;

namespace Terminus.Tests.Service;

public class SeasonServiceTests
{
    // Petak 25.12.2026. i nedjelja 1.11.2026.
    private static readonly DateOnly Christmas = new(2026, 12, 25);
    private static readonly DateOnly AllSaints = new(2026, 11, 1);

    private readonly FakeSeasonRepository _repository = new();
    private readonly FakeHolidaySource _holidays = new();
    private readonly SeasonService _service;

    public SeasonServiceTests()
    {
        _holidays.Holidays.Add(new PublicHoliday(AllSaints, "Svi sveti"));
        _holidays.Holidays.Add(new PublicHoliday(Christmas, "Božić"));
        var agency = new AgencyModel { Id = Guid.NewGuid(), Name = "Test", Timezone = "Europe/Zagreb" };
        _service = new SeasonService(_repository, new FakeAgencyRepository(agency), _holidays);
    }

    [Fact]
    public async Task CreateAsync_DayTypesCoverTheSeason()
    {
        var season = await CreateAsync();

        var dayTypes = season.State.DayTypes;
        Assert.Equal(3, dayTypes.Count);
        Assert.All(dayTypes, d =>
        {
            Assert.Equal(season.State.Season.StartDate, d.StartDate);
            Assert.Equal(season.State.Season.EndDate, d.EndDate);
            Assert.False(string.IsNullOrEmpty(d.Code));
        });
        Assert.True(dayTypes[0].Monday && dayTypes[0].Friday && !dayTypes[0].Saturday);
    }

    [Fact]
    public async Task CreateAsync_HolidayRunsAsSunday_BecomesCalendarExceptions()
    {
        var season = await CreateAsync();
        var (workday, sunday) = (season.State.DayTypes[0], season.State.DayTypes[2]);

        var day = Assert.Single(season.State.Days, d => d.Date == Christmas);
        Assert.Equal(SeasonDaySource.Holiday, day.Source);
        Assert.Equal("Božić", day.Note);
        Assert.Equal(sunday.Id, day.CalendarId);

        Assert.Contains(_repository.CalendarDates, d =>
            d.ServiceCalendarId == workday.Id && d.Date == Christmas && d.ExceptionType == CalendarExceptionType.Removed);
        Assert.Contains(_repository.CalendarDates, d =>
            d.ServiceCalendarId == sunday.Id && d.Date == Christmas && d.ExceptionType == CalendarExceptionType.Added);
    }

    [Fact]
    public async Task CreateAsync_HolidayOnSunday_IsShownButChangesNothing()
    {
        var season = await CreateAsync();

        Assert.Contains(season.State.Days, d => d.Date == AllSaints && d.Note == "Svi sveti");
        Assert.DoesNotContain(_repository.CalendarDates, d => d.Date == AllSaints);
    }

    [Fact]
    public async Task CreateAsync_WeekdayInTwoDayTypes_IsRejected()
    {
        var error = await Assert.ThrowsAsync<ValidationException>(() => CreateAsync(dayTypes:
        [
            DayType("Radni dan", DayOfWeek.Monday, DayOfWeek.Tuesday),
            DayType("Utorak", DayOfWeek.Tuesday)
        ]));

        Assert.Contains("DayTypes", error.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_OverlappingSeason_IsConflict()
    {
        await CreateAsync();

        await Assert.ThrowsAsync<ConflictException>(() => CreateAsync(start: new DateOnly(2027, 6, 1), end: new DateOnly(2027, 8, 31)));
    }

    [Fact]
    public async Task CreateAsync_HolidaysUnavailable_IsUnprocessable()
    {
        _holidays.Unavailable = true;

        await Assert.ThrowsAsync<UnprocessableException>(() => CreateAsync());
    }

    [Fact]
    public async Task CreateAsync_WithoutCountry_HasNoHolidays()
    {
        var season = await CreateAsync(country: null, mode: SeasonDayMode.Regular);

        Assert.Empty(season.State.Days);
        Assert.Empty(_repository.CalendarDates);
    }

    [Fact]
    public async Task SetDayAsync_ManualDayWinsOverHolidayRule()
    {
        var season = await CreateAsync();
        var id = season.State.Season.Id;

        await _service.SetDayAsync(id, Christmas, SeasonDayMode.NoService, null, "Ne vozi", TestContext.Current.CancellationToken);
        var updated = await _service.UpdateAsync(id, Copy(season.State.Season, SeasonDayMode.Regular), TestContext.Current.CancellationToken);

        var day = Assert.Single(updated.State.Days, d => d.Date == Christmas);
        Assert.Equal(SeasonDaySource.Manual, day.Source);
        Assert.Equal(SeasonDayMode.NoService, day.Mode);
        Assert.All(_repository.CalendarDates.Where(d => d.Date == Christmas), d => Assert.Equal(CalendarExceptionType.Removed, d.ExceptionType));
    }

    [Fact]
    public async Task ResetDayAsync_HolidayRuleAppliesAgain()
    {
        var season = await CreateAsync();
        var id = season.State.Season.Id;
        await _service.SetDayAsync(id, Christmas, SeasonDayMode.Regular, null, null, TestContext.Current.CancellationToken);

        var reset = await _service.ResetDayAsync(id, Christmas, TestContext.Current.CancellationToken);

        var day = Assert.Single(reset.State.Days, d => d.Date == Christmas);
        Assert.Equal(SeasonDaySource.Holiday, day.Source);
        Assert.Equal(SeasonDayMode.RunsAs, day.Mode);
    }

    [Fact]
    public async Task SetDayAsync_OutsideSeason_IsRejected()
    {
        var season = await CreateAsync();

        await Assert.ThrowsAsync<ValidationException>(() => _service.SetDayAsync(
            season.State.Season.Id, new DateOnly(2030, 1, 1), SeasonDayMode.NoService, null, null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveDayTypesAsync_RemovingDayTypeWithTrips_IsConflict()
    {
        var season = await CreateAsync(mode: SeasonDayMode.NoService);
        var saturday = season.State.DayTypes[1];
        _repository.TripCounts[saturday.Id] = 12;

        var kept = season.State.DayTypes.Where(d => d.Id != saturday.Id).ToList();

        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.SaveDayTypesAsync(season.State.Season.Id, kept, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveDayTypesAsync_RemovingHolidayDayType_IsRejected()
    {
        var season = await CreateAsync();
        var kept = season.State.DayTypes.Take(2).ToList();

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.SaveDayTypesAsync(season.State.Season.Id, kept, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveDayTypesAsync_NewDayTypeGetsCodeAndSeasonPeriod()
    {
        var season = await CreateAsync(dayTypes: [DayType("Radni dan", DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday), DayType("Nedjelja", DayOfWeek.Sunday)],
            holidayIndex: 1);

        var dayTypes = season.State.DayTypes.Append(DayType("Petak", DayOfWeek.Friday)).ToList();
        var saved = await _service.SaveDayTypesAsync(season.State.Season.Id, dayTypes, TestContext.Current.CancellationToken);

        var friday = Assert.Single(saved.State.DayTypes, d => d.Name == "Petak");
        Assert.NotEqual(Guid.Empty, friday.Id);
        Assert.Equal(season.State.Season.EndDate, friday.EndDate);
        // Božić je petak: sad ga petak predaje nedjelji.
        Assert.Contains(_repository.CalendarDates, d => d.ServiceCalendarId == friday.Id && d.Date == Christmas);
    }

    [Fact]
    public async Task CopyAsync_CopiesDayTypesHolidayRuleAndTrips()
    {
        var source = await CreateAsync();
        var sourceIds = source.State.DayTypes.Select(d => d.Id).ToList();

        var copy = await _service.CopyAsync(
            source.State.Season.Id, Period("Zimski 2027/28", new DateOnly(2027, 9, 1), new DateOnly(2028, 6, 14)),
            withTrips: true, TestContext.Current.CancellationToken);

        var season = copy.State.Season;
        Assert.NotEqual(source.State.Season.Id, season.Id);
        Assert.Equal("HR", season.HolidayCountry);
        Assert.Equal(SeasonDayMode.RunsAs, season.HolidayMode);
        Assert.Equal(["Radni dan", "Subota", "Nedjelja"], copy.State.DayTypes.Select(d => d.Name));
        Assert.All(copy.State.DayTypes, d =>
        {
            Assert.DoesNotContain(d.Id, sourceIds);
            Assert.Equal(season.EndDate, d.EndDate);
        });
        // Praznici voze kao nedjelja i u kopiji.
        Assert.Equal(copy.State.DayTypes[2].Id, season.HolidayCalendarId);

        var copied = _repository.CopiedTrips!;
        Assert.Equal(sourceIds.Order(), copied.Keys.Order());
        Assert.Equal(copy.State.DayTypes[1].Id, copied[sourceIds[1]]);
    }

    [Fact]
    public async Task CopyAsync_WithoutTrips_CopiesNoTrips()
    {
        var source = await CreateAsync();

        await _service.CopyAsync(
            source.State.Season.Id, Period("Ljeto 2027", new DateOnly(2027, 6, 15), new DateOnly(2027, 8, 31)),
            withTrips: false, TestContext.Current.CancellationToken);

        Assert.Null(_repository.CopiedTrips);
    }

    [Fact]
    public async Task CopyAsync_OverlappingPeriod_IsConflict()
    {
        var source = await CreateAsync();

        await Assert.ThrowsAsync<ConflictException>(() => _service.CopyAsync(
            source.State.Season.Id, Period("Kopija", new DateOnly(2027, 6, 1), new DateOnly(2027, 8, 31)),
            withTrips: true, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SavePeriodsAsync_WorkingDaysOfPeriodRunAsReplacement()
    {
        var (season, school, holiday) = await CreateWithBreakAsync();

        var saved = await _service.SavePeriodsAsync(season.State.Season.Id,
            [Period("Zimski raspust", new DateOnly(2026, 12, 21), new DateOnly(2027, 1, 8), school, holiday)],
            TestContext.Current.CancellationToken);

        // Utorak u raspustu vozi kao raspust.
        var tuesday = Assert.Single(saved.State.Days, d => d.Date == new DateOnly(2026, 12, 22));
        Assert.Equal(SeasonDaySource.Period, tuesday.Source);
        Assert.Equal(holiday, tuesday.CalendarId);
        Assert.Equal("Zimski raspust", tuesday.Note);
        Assert.Contains(_repository.CalendarDates, d => d.ServiceCalendarId == holiday && d.Date == tuesday.Date && d.ExceptionType == CalendarExceptionType.Added);
        Assert.Contains(_repository.CalendarDates, d => d.ServiceCalendarId == school && d.Date == tuesday.Date && d.ExceptionType == CalendarExceptionType.Removed);
        // Subota ostaje subota, a Božić vozi po pravilu za praznike.
        Assert.DoesNotContain(saved.State.Days, d => d.Date == new DateOnly(2026, 12, 26));
        Assert.Equal(SeasonDaySource.Holiday, Assert.Single(saved.State.Days, d => d.Date == Christmas).Source);
    }

    [Fact]
    public async Task ResetDayAsync_PeriodAppliesAgain()
    {
        var (season, school, holiday) = await CreateWithBreakAsync();
        var id = season.State.Season.Id;
        var day = new DateOnly(2026, 12, 22);
        await _service.SavePeriodsAsync(id, [Period("Raspust", new DateOnly(2026, 12, 21), new DateOnly(2027, 1, 8), school, holiday)],
            TestContext.Current.CancellationToken);

        var manual = await _service.SetDayAsync(id, day, SeasonDayMode.NoService, null, "Štrajk", TestContext.Current.CancellationToken);
        Assert.Equal(SeasonDaySource.Manual, Assert.Single(manual.State.Days, d => d.Date == day).Source);

        var reset = await _service.ResetDayAsync(id, day, TestContext.Current.CancellationToken);
        Assert.Equal(SeasonDaySource.Period, Assert.Single(reset.State.Days, d => d.Date == day).Source);
    }

    [Fact]
    public async Task SavePeriodsAsync_OverlappingPeriodsOfSameDayType_AreRejected()
    {
        var (season, school, holiday) = await CreateWithBreakAsync();

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.SavePeriodsAsync(season.State.Season.Id,
            [
                Period("Zimski", new DateOnly(2026, 12, 21), new DateOnly(2027, 1, 8), school, holiday),
                Period("Novogodišnji", new DateOnly(2027, 1, 4), new DateOnly(2027, 1, 15), school, holiday)
            ],
            TestContext.Current.CancellationToken));

        Assert.Contains("Periods", ex.Errors.Keys);
    }

    [Fact]
    public async Task SavePeriodsAsync_OutsideSeason_IsRejected()
    {
        var (season, school, holiday) = await CreateWithBreakAsync();

        await Assert.ThrowsAsync<ValidationException>(() => _service.SavePeriodsAsync(season.State.Season.Id,
            [Period("Ljeto", new DateOnly(2027, 6, 15), new DateOnly(2027, 8, 31), school, holiday)],
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveDayTypesAsync_DayTypeUsedByPeriod_IsRejected()
    {
        var (season, school, holiday) = await CreateWithBreakAsync();
        var id = season.State.Season.Id;
        await _service.SavePeriodsAsync(id, [Period("Raspust", new DateOnly(2026, 12, 21), new DateOnly(2027, 1, 8), school, holiday)],
            TestContext.Current.CancellationToken);

        var kept = season.State.DayTypes.Where(d => d.Id != holiday).ToList();

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.SaveDayTypesAsync(id, kept, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CopyAsync_MovesPeriodsWithTheSeason()
    {
        var (season, school, holiday) = await CreateWithBreakAsync();
        await _service.SavePeriodsAsync(season.State.Season.Id,
            [Period("Zimski raspust", new DateOnly(2026, 12, 21), new DateOnly(2027, 1, 8), school, holiday)],
            TestContext.Current.CancellationToken);

        var copy = await _service.CopyAsync(season.State.Season.Id,
            Period("Zimski 2027/28", new DateOnly(2027, 9, 1), new DateOnly(2028, 6, 14)),
            withTrips: false, TestContext.Current.CancellationToken);

        var period = Assert.Single(copy.State.Periods);
        Assert.Equal(new DateOnly(2027, 12, 21), period.StartDate);
        Assert.Equal(new DateOnly(2028, 1, 8), period.EndDate);
        Assert.Equal(copy.State.DayTypes.Single(d => d.Name == "Radni dan").Id, period.ReplacedCalendarId);
        Assert.Equal(copy.State.DayTypes.Single(d => d.Name == "Raspust").Id, period.CalendarId);
    }

    [Fact]
    public async Task GetHolidayCountriesAsync_SuggestsCountryFromAgencyTimezone()
    {
        var countries = await _service.GetHolidayCountriesAsync(TestContext.Current.CancellationToken);

        Assert.Equal("HR", countries.Suggested);
    }

    private Task<SeasonDetail> CreateAsync(
        DateOnly? start = null,
        DateOnly? end = null,
        string? country = "HR",
        SeasonDayMode mode = SeasonDayMode.RunsAs,
        IReadOnlyList<IServiceCalendarModel>? dayTypes = null,
        int? holidayIndex = 2)
    {
        var season = new TimetableSeasonModel
        {
            Name = "Zimski 2026/27",
            StartDate = start ?? new DateOnly(2026, 9, 1),
            EndDate = end ?? new DateOnly(2027, 6, 14),
            HolidayCountry = country,
            HolidayMode = mode
        };

        dayTypes ??=
        [
            DayType("Radni dan", DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday),
            DayType("Subota", DayOfWeek.Saturday),
            DayType("Nedjelja", DayOfWeek.Sunday)
        ];

        return _service.CreateAsync(season, dayTypes, holidayIndex, TestContext.Current.CancellationToken);
    }

    /// <summary>Sezona s radnim danom za vrijeme nastave i tipom dana za raspust (bez dana u tjednu).</summary>
    private async Task<(SeasonDetail Season, Guid School, Guid Holiday)> CreateWithBreakAsync()
    {
        var season = await CreateAsync(dayTypes:
        [
            DayType("Radni dan", DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday),
            DayType("Raspust"),
            DayType("Vikend", DayOfWeek.Saturday, DayOfWeek.Sunday)
        ]);
        var dayTypes = season.State.DayTypes;
        return (season, dayTypes.Single(d => d.Name == "Radni dan").Id, dayTypes.Single(d => d.Name == "Raspust").Id);
    }

    private static SeasonPeriod Period(string name, DateOnly start, DateOnly end, Guid replaced, Guid runsAs) =>
        new(Guid.Empty, name, start, end, replaced, SeasonDayMode.RunsAs, runsAs);

    private static ITimetableSeasonModel Period(string name, DateOnly start, DateOnly end) => new TimetableSeasonModel
    {
        Name = name,
        StartDate = start,
        EndDate = end
    };

    private static ITimetableSeasonModel Copy(ITimetableSeasonModel season, SeasonDayMode mode) => new TimetableSeasonModel
    {
        Name = season.Name,
        StartDate = season.StartDate,
        EndDate = season.EndDate,
        HolidayCountry = season.HolidayCountry,
        HolidayMode = mode,
        HolidayCalendarId = season.HolidayCalendarId
    };

    private static IServiceCalendarModel DayType(string name, params DayOfWeek[] days) => new ServiceCalendarModel
    {
        Name = name,
        Monday = days.Contains(DayOfWeek.Monday),
        Tuesday = days.Contains(DayOfWeek.Tuesday),
        Wednesday = days.Contains(DayOfWeek.Wednesday),
        Thursday = days.Contains(DayOfWeek.Thursday),
        Friday = days.Contains(DayOfWeek.Friday),
        Saturday = days.Contains(DayOfWeek.Saturday),
        Sunday = days.Contains(DayOfWeek.Sunday)
    };
}
