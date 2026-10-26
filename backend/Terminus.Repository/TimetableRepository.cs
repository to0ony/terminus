using Microsoft.EntityFrameworkCore;
using Terminus.DAL;
using Terminus.DAL.Entities;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Repository.Common;
using Terminus.Repository.Mapping;

namespace Terminus.Repository;

public class TimetableRepository(TerminusDbContext context) : ITimetableRepository
{
    public Task<IReadOnlyList<TripStopTimes>> GetRouteTripsAsync(
        Guid routeId,
        TripDirection? directionId,
        IReadOnlyCollection<Guid>? serviceCalendarIds,
        CancellationToken cancellationToken = default) =>
        LoadAsync(routeId, directionId, serviceCalendarIds, cancellationToken);

    public Task<IReadOnlyList<TripStopTimes>> GetTripsAsync(
        IReadOnlyCollection<Guid> serviceCalendarIds,
        CancellationToken cancellationToken = default) =>
        LoadAsync(null, null, serviceCalendarIds, cancellationToken);

    private async Task<IReadOnlyList<TripStopTimes>> LoadAsync(
        Guid? routeId,
        TripDirection? directionId,
        IReadOnlyCollection<Guid>? serviceCalendarIds,
        CancellationToken cancellationToken)
    {
        var trips = await RouteTrips(routeId, directionId, serviceCalendarIds)
            .ToListAsync(cancellationToken);

        // Vremena odvojenim upitom: jedan JOIN bi ponavljao svaki redak vožnje uz svako vrijeme.
        var stopTimes = await RouteTrips(routeId, directionId, serviceCalendarIds)
            .SelectMany(t => t.StopTimes)
            .OrderBy(st => st.TripId)
            .ThenBy(st => st.StopSequence)
            .ToListAsync(cancellationToken);

        var stopTimesByTrip = stopTimes
            .GroupBy(st => st.TripId)
            .ToDictionary(g => g.Key, g => g.Select(TripMapper.ToModel).ToList());

        return trips
            .Select(t => new TripStopTimes(
                TripMapper.ToModel(t),
                stopTimesByTrip.GetValueOrDefault(t.Id) ?? []))
            .ToList();
    }

    public async Task<IReadOnlyList<IRouteModel>> GetRoutesServingAsync(
        IReadOnlyCollection<Guid> stopIds,
        CancellationToken cancellationToken = default)
    {
        var entities = await context.Routes
            .AsNoTracking()
            .Where(r => r.Trips.Any(t => t.StopTimes.Any(st => stopIds.Contains(st.StopId))))
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.ShortName!.Length)
            .ThenBy(r => r.ShortName)
            .ThenBy(r => r.LongName)
            .ThenBy(r => r.Code)
            .ToListAsync(cancellationToken);

        return entities.Select(RouteMapper.ToModel).ToList();
    }

    /// <param name="routeId">Samo vožnje te linije; null za sve linije.</param>
    private IQueryable<Trip> RouteTrips(Guid? routeId, TripDirection? directionId, IReadOnlyCollection<Guid>? serviceCalendarIds)
    {
        var query = context.Trips.AsNoTracking();
        if (routeId is { } route)
        {
            query = query.Where(t => t.RouteId == route);
        }

        if (directionId is { } direction)
        {
            var value = (short)direction;
            query = query.Where(t => t.DirectionId == value);
        }

        if (serviceCalendarIds is not null)
        {
            query = query.Where(t => serviceCalendarIds.Contains(t.ServiceCalendarId));
        }

        return query;
    }
}
