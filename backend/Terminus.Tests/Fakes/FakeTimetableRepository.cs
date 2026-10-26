using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Repository.Common;

namespace Terminus.Tests.Fakes;

/// <summary>Vožnje s vremenima u memoriji, filtrirane kao u pravom upitu.</summary>
internal sealed class FakeTimetableRepository : ITimetableRepository
{
    public List<TripStopTimes> Trips { get; } = [];
    public List<IRouteModel> Routes { get; } = [];

    public Task<IReadOnlyList<TripStopTimes>> GetRouteTripsAsync(
        Guid routeId,
        TripDirection? directionId,
        IReadOnlyCollection<Guid>? serviceCalendarIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TripStopTimes>>(Trips
            .Where(t => t.Trip.RouteId == routeId
                && (directionId is null || t.Trip.DirectionId == directionId)
                && (serviceCalendarIds is null || serviceCalendarIds.Contains(t.Trip.ServiceCalendarId)))
            .ToList());

    public Task<IReadOnlyList<TripStopTimes>> GetTripsAsync(
        IReadOnlyCollection<Guid> serviceCalendarIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TripStopTimes>>(Trips
            .Where(t => serviceCalendarIds.Contains(t.Trip.ServiceCalendarId))
            .ToList());

    public Task<IReadOnlyList<IRouteModel>> GetRoutesServingAsync(
        IReadOnlyCollection<Guid> stopIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IRouteModel>>(Routes
            .Where(r => Trips.Any(t => t.Trip.RouteId == r.Id && t.StopTimes.Any(st => stopIds.Contains(st.StopId))))
            .ToList());
}
