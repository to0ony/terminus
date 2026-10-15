using Terminus.Model.Common;
using Terminus.Repository.Common;

namespace Terminus.Tests.Fakes;

/// <summary>Podaci za GTFS izvoz u memoriji, vraćaju se onako kako su dodani.</summary>
internal sealed class FakeGtfsExportRepository : IGtfsExportRepository
{
    public List<IAgencyModel> Agencies { get; } = [];
    public List<IStopModel> Stops { get; } = [];
    public List<IRouteModel> Routes { get; } = [];
    public List<IServiceCalendarModel> Calendars { get; } = [];
    public List<ICalendarDateModel> CalendarDates { get; } = [];
    public List<ITripModel> Trips { get; } = [];
    public List<IStopTimeModel> StopTimes { get; } = [];

    public Task<IReadOnlyList<IAgencyModel>> GetAgenciesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IAgencyModel>>(Agencies);

    public Task<IReadOnlyList<IStopModel>> GetStopsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IStopModel>>(Stops);

    public Task<IReadOnlyList<IRouteModel>> GetRoutesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IRouteModel>>(Routes);

    public Task<IReadOnlyList<IServiceCalendarModel>> GetServiceCalendarsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IServiceCalendarModel>>(Calendars);

    public Task<IReadOnlyList<ICalendarDateModel>> GetCalendarDatesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ICalendarDateModel>>(CalendarDates);

    public Task<IReadOnlyList<ITripModel>> GetTripsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ITripModel>>(Trips);

    public Task<IReadOnlyList<IStopTimeModel>> GetStopTimesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IStopTimeModel>>(StopTimes);
}
