using Microsoft.EntityFrameworkCore;
using Terminus.DAL;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Repository.Common;
using Terminus.Repository.Mapping;

namespace Terminus.Repository;

public class DepartureRepository(TerminusDbContext context) : IDepartureRepository
{
    public async Task<IAgencyModel?> GetPrimaryAgencyAsync(CancellationToken cancellationToken = default)
    {
        var entity = await context.Agencies
            .AsNoTracking()
            .OrderBy(a => a.CreatedAt)
            .ThenBy(a => a.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : AgencyMapper.ToModel(entity);
    }

    public async Task<IReadOnlyList<IStopModel>> GetPlatformsAsync(Guid stationId, CancellationToken cancellationToken = default)
    {
        var entities = await context.Stops
            .AsNoTracking()
            .Where(s => s.ParentStationId == stationId)
            .ToListAsync(cancellationToken);

        return entities.Select(StopMapper.ToModel).ToList();
    }

    public async Task<IReadOnlyList<IServiceCalendarModel>> GetCalendarsAsync(CancellationToken cancellationToken = default)
    {
        var entities = await context.ServiceCalendars
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return entities.Select(ServiceCalendarMapper.ToModel).ToList();
    }

    public async Task<IReadOnlyList<ICalendarDateModel>> GetCalendarDatesAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var entities = await context.CalendarDates
            .AsNoTracking()
            .Where(d => d.Date >= from && d.Date <= to)
            .ToListAsync(cancellationToken);

        return entities.Select(ServiceCalendarMapper.ToModel).ToList();
    }

    public async Task<IReadOnlyList<ScheduledDeparture>> GetScheduledAsync(
        IReadOnlyCollection<Guid> stopIds,
        IReadOnlyCollection<Guid> serviceCalendarIds,
        int fromSeconds,
        int toSeconds,
        int limit,
        CancellationToken cancellationToken = default) =>
        await ScheduledQuery(context, stopIds, serviceCalendarIds, fromSeconds, toSeconds, limit)
            .ToListAsync(cancellationToken);

    /// <summary>Izdvojeno da test može provjeriti prevodi li EF upit u SQL.</summary>
    internal static IQueryable<ScheduledDeparture> ScheduledQuery(
        TerminusDbContext context,
        IReadOnlyCollection<Guid> stopIds,
        IReadOnlyCollection<Guid> serviceCalendarIds,
        int fromSeconds,
        int toSeconds,
        int limit)
    {
        const short noPickup = (short)PickupDropOffType.None;

        // Koristi ix_stop_times_stop_departure (stop_id, departure_time).
        return context.StopTimes
            .AsNoTracking()
            .Where(st => stopIds.Contains(st.StopId)
                && st.DepartureTime >= fromSeconds
                && st.DepartureTime <= toSeconds
                && st.PickupType != noPickup
                && serviceCalendarIds.Contains(st.Trip.ServiceCalendarId)
                && st.Trip.Route.IsActive
                // Na zadnjoj stanici vožnja samo stiže.
                && st.Trip.StopTimes.Any(next => next.StopSequence > st.StopSequence))
            .OrderBy(st => st.DepartureTime)
            .ThenBy(st => st.TripId)
            .Take(limit)
            .Select(st => new ScheduledDeparture(
                st.TripId,
                st.Trip.ServiceCalendarId,
                st.Trip.RouteId,
                st.Trip.Route.ShortName,
                st.Trip.Route.LongName,
                st.Trip.Route.Color,
                st.Trip.Route.TextColor,
                st.StopHeadsign
                    ?? st.Trip.Headsign
                    ?? st.Trip.StopTimes
                        .OrderByDescending(last => last.StopSequence)
                        .Select(last => last.Stop.Name)
                        .First(),
                st.StopId,
                st.Stop.PlatformCode,
                st.DepartureTime));
    }
}
