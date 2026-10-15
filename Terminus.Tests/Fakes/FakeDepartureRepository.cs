using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Repository.Common;

namespace Terminus.Tests.Fakes;

/// <summary>
/// Vozni red u memoriji. GetScheduledAsync filtrira po stanicama, kalendarima i
/// rasponu vremena kao pravi upit; pravila koja upit provodi u bazi (zadnja
/// stanica, bez ukrcavanja, neaktivna linija) test zadaje već filtriranim podacima.
/// </summary>
internal sealed class FakeDepartureRepository : IDepartureRepository
{
    public IAgencyModel? Agency { get; set; } = new AgencyModel
    {
        Id = Guid.NewGuid(),
        Name = "Test prijevoznik d.o.o.",
        Url = "https://example.com",
        Timezone = "Europe/Zagreb",
        Lang = "hr"
    };
    public List<IStopModel> Stops { get; } = [];
    public List<IServiceCalendarModel> Calendars { get; } = [];
    public List<ICalendarDateModel> CalendarDates { get; } = [];
    public List<ScheduledDeparture> Departures { get; } = [];

    public Task<IAgencyModel?> GetPrimaryAgencyAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Agency);

    public Task<IReadOnlyList<IStopModel>> GetPlatformsAsync(Guid stationId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IStopModel>>(Stops.Where(s => s.ParentStationId == stationId).ToList());

    public Task<IReadOnlyList<IServiceCalendarModel>> GetCalendarsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IServiceCalendarModel>>(Calendars);

    public Task<IReadOnlyList<ICalendarDateModel>> GetCalendarDatesAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ICalendarDateModel>>(CalendarDates.Where(d => d.Date >= from && d.Date <= to).ToList());

    public Task<IReadOnlyList<ScheduledDeparture>> GetScheduledAsync(
        IReadOnlyCollection<Guid> stopIds,
        IReadOnlyCollection<Guid> serviceCalendarIds,
        int fromSeconds,
        int toSeconds,
        int limit,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ScheduledDeparture>>(Departures
            .Where(d => stopIds.Contains(d.StopId)
                && serviceCalendarIds.Contains(d.ServiceCalendarId)
                && d.DepartureTime >= fromSeconds
                && d.DepartureTime <= toSeconds)
            .OrderBy(d => d.DepartureTime)
            .Take(limit)
            .ToList());
}
