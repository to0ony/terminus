using Terminus.Common;
using Terminus.Common.Exceptions;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Service;
using Terminus.Service.Common;
using Terminus.Tests.Fakes;

namespace Terminus.Tests.Service;

public class DepartureServiceTests
{
    private static readonly TimeSpan Winter = TimeSpan.FromHours(1);

    private readonly FakeDepartureRepository _repository = new();
    private readonly StopModel _stop = Stop("OS-AK-3", "3");
    private readonly ServiceCalendarModel _workdays = Calendar(monday: true, tuesday: true, wednesday: true, thursday: true, friday: true);

    public DepartureServiceTests()
    {
        _repository.Calendars.Add(_workdays);
    }

    [Fact]
    public async Task GetNextAsync_ReturnsUpcomingDeparturesInOrder()
    {
        AddDeparture(_stop, _workdays, "06:30:00");
        AddDeparture(_stop, _workdays, "07:15:00");
        AddDeparture(_stop, _workdays, "07:05:00");

        // Srijeda 23.12.2026. u 06:58. Polazak u 06:30 je prošao; sljedeći put je
        // sutra (četvrtak), unutar 24 h unaprijed.
        var result = await GetNextAsync(_stop, At(2026, 12, 23, 6, 58));

        Assert.Equal(
            [At(2026, 12, 23, 7, 5), At(2026, 12, 23, 7, 15), At(2026, 12, 24, 6, 30)],
            result.Departures.Select(d => d.DepartureTime));
        Assert.Equal(
            [new DateOnly(2026, 12, 23), new DateOnly(2026, 12, 23), new DateOnly(2026, 12, 24)],
            result.Departures.Select(d => d.ServiceDate));
    }

    [Fact]
    public async Task GetNextAsync_DepartureExactlyNow_IsIncluded()
    {
        AddDeparture(_stop, _workdays, "07:05:00");

        var result = await GetNextAsync(_stop, At(2026, 12, 23, 7, 5));

        Assert.Equal(At(2026, 12, 23, 7, 5), result.Departures[0].DepartureTime);
    }

    [Fact]
    public async Task GetNextAsync_RespectsLimit()
    {
        AddDeparture(_stop, _workdays, "07:05:00");
        AddDeparture(_stop, _workdays, "07:15:00");
        AddDeparture(_stop, _workdays, "07:25:00");

        var result = await GetNextAsync(_stop, At(2026, 12, 23, 6, 58), limit: 2);

        Assert.Equal(2, result.Departures.Count);
    }

    [Fact]
    public async Task GetNextAsync_AfterMidnight_IncludesPreviousServiceDay()
    {
        AddDeparture(_stop, _workdays, "23:50:00");
        AddDeparture(_stop, _workdays, "24:20:00");

        // Četvrtak 24.12. u 00:10: vožnja srijede u 24:20 kreće za 10 minuta.
        var result = await GetNextAsync(_stop, At(2026, 12, 24, 0, 10));

        var first = result.Departures[0];
        Assert.Equal(At(2026, 12, 24, 0, 20), first.DepartureTime);
        Assert.Equal(new DateOnly(2026, 12, 23), first.ServiceDate);
    }

    [Fact]
    public async Task GetNextAsync_LateEvening_IncludesTomorrowMorning()
    {
        AddDeparture(_stop, _workdays, "06:00:00");

        var result = await GetNextAsync(_stop, At(2026, 12, 23, 22, 0));

        var departure = Assert.Single(result.Departures);
        Assert.Equal(At(2026, 12, 24, 6, 0), departure.DepartureTime);
        Assert.Equal(new DateOnly(2026, 12, 24), departure.ServiceDate);
    }

    [Fact]
    public async Task GetNextAsync_RemovedDate_HasNoDepartures()
    {
        _repository.CalendarDates.Add(new CalendarDateModel
        {
            ServiceCalendarId = _workdays.Id,
            Date = new DateOnly(2026, 12, 25),
            ExceptionType = CalendarExceptionType.Removed
        });
        AddDeparture(_stop, _workdays, "07:05:00");

        // Petak 25.12. ne vozi, a subota 26.12. nije radni dan.
        var result = await GetNextAsync(_stop, At(2026, 12, 25, 6, 0));

        Assert.Empty(result.Departures);
    }

    [Fact]
    public async Task GetNextAsync_FallBackDay_UsesWallClockTime()
    {
        var everyDay = Calendar(monday: true, tuesday: true, wednesday: true, thursday: true, friday: true, saturday: true, sunday: true);
        _repository.Calendars.Add(everyDay);
        AddDeparture(_stop, everyDay, "06:00:00");

        // Nedjelja 25.10.2026., sat je u 03:00 vraćen na 02:00.
        var result = await GetNextAsync(_stop, At(2026, 10, 25, 5, 0));

        Assert.Equal(At(2026, 10, 25, 6, 0), result.Departures[0].DepartureTime);
    }

    [Fact]
    public async Task GetNextAsync_Station_ReturnsDeparturesFromAllPlatforms()
    {
        var station = Stop("OS-AK", platformCode: null, LocationType.Station);
        var platform1 = Stop("OS-AK-1", "1", parent: station);
        var platform3 = Stop("OS-AK-3", "3", parent: station);
        var elsewhere = Stop("DJ-AK", "1");
        _repository.Stops.AddRange([platform1, platform3, elsewhere]);

        AddDeparture(platform3, _workdays, "07:15:00");
        AddDeparture(platform1, _workdays, "07:05:00");
        AddDeparture(elsewhere, _workdays, "07:10:00");

        var result = await GetNextAsync(station, At(2026, 12, 23, 6, 58), stops: [station, platform1, platform3, elsewhere]);

        Assert.Equal(["1", "3"], result.Departures.Select(d => d.PlatformCode));
        Assert.Equal(station.Id, result.Stop.Id);
    }

    [Fact]
    public async Task GetNextAsync_ReturnsNowInAgencyTimeZone()
    {
        var result = await GetNextAsync(_stop, new DateTimeOffset(2026, 12, 23, 5, 58, 0, TimeSpan.Zero));

        Assert.Equal(Winter, result.Now.Offset);
        Assert.Equal(6, result.Now.Hour);
    }

    [Fact]
    public async Task GetNextAsync_UnknownStop_ThrowsNotFound()
    {
        var service = new DepartureService(_repository, new FakeStopRepository([]));

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.GetNextAsync(Guid.NewGuid(), At(2026, 12, 23, 7, 0), 10, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetNextAsync_NoAgency_ThrowsUnprocessable()
    {
        _repository.Timezone = null;

        await Assert.ThrowsAsync<UnprocessableException>(() => GetNextAsync(_stop, At(2026, 12, 23, 7, 0)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(DepartureService.MaxLimit + 1)]
    public async Task GetNextAsync_InvalidLimit_ThrowsValidationError(int limit)
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() => GetNextAsync(_stop, At(2026, 12, 23, 7, 0), limit));

        Assert.Contains("Limit", ex.Errors.Keys);
    }

    private Task<StopDepartures> GetNextAsync(
        StopModel stop,
        DateTimeOffset now,
        int limit = 10,
        IEnumerable<IStopModel>? stops = null)
    {
        var service = new DepartureService(_repository, new FakeStopRepository(stops ?? [stop]));
        return service.GetNextAsync(stop.Id, now, limit, TestContext.Current.CancellationToken);
    }

    private void AddDeparture(IStopModel stop, IServiceCalendarModel calendar, string time)
    {
        Assert.True(GtfsTime.TryParse(time, out var seconds));
        _repository.Departures.Add(new ScheduledDeparture(
            TripId: Guid.NewGuid(),
            ServiceCalendarId: calendar.Id,
            RouteId: Guid.NewGuid(),
            RouteShortName: "12",
            RouteLongName: "Osijek - Đakovo",
            RouteColor: "1565C0",
            RouteTextColor: "FFFFFF",
            Headsign: "Đakovo",
            StopId: stop.Id,
            PlatformCode: stop.PlatformCode,
            DepartureTime: seconds));
    }

    /// <summary>Lokalno vrijeme u Zagrebu (CET, odnosno CEST od zadnje nedjelje u ožujku do zadnje u listopadu).</summary>
    private static DateTimeOffset At(int year, int month, int day, int hour, int minute)
    {
        var local = new DateTime(year, month, day, hour, minute, 0);
        var zagreb = TimeZoneInfo.FindSystemTimeZoneById("Europe/Zagreb");
        return new DateTimeOffset(local, zagreb.GetUtcOffset(local));
    }

    private static StopModel Stop(
        string code,
        string? platformCode,
        LocationType locationType = LocationType.StopOrPlatform,
        StopModel? parent = null) => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        Name = code,
        PlatformCode = platformCode,
        LocationType = locationType,
        ParentStationId = parent?.Id
    };

    private static ServiceCalendarModel Calendar(
        bool monday = false,
        bool tuesday = false,
        bool wednesday = false,
        bool thursday = false,
        bool friday = false,
        bool saturday = false,
        bool sunday = false) => new()
    {
        Id = Guid.NewGuid(),
        Monday = monday,
        Tuesday = tuesday,
        Wednesday = wednesday,
        Thursday = thursday,
        Friday = friday,
        Saturday = saturday,
        Sunday = sunday,
        StartDate = new DateOnly(2026, 1, 1),
        EndDate = new DateOnly(2026, 12, 31)
    };
}
