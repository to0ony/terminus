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
