using Microsoft.EntityFrameworkCore;
using Terminus.DAL;
using Terminus.Model.Common;
using Terminus.Repository.Common;
using Terminus.Repository.Mapping;

namespace Terminus.Repository;

public class GtfsExportRepository(TerminusDbContext context) : IGtfsExportRepository
{
    public async Task<IReadOnlyList<IAgencyModel>> GetAgenciesAsync(CancellationToken cancellationToken = default)
    {
        var entities = await context.Agencies
            .AsNoTracking()
            .OrderBy(a => a.CreatedAt)
            .ThenBy(a => a.Id)
            .ToListAsync(cancellationToken);

        return entities.Select(AgencyMapper.ToModel).ToList();
    }

    public async Task<IReadOnlyList<IStopModel>> GetStopsAsync(CancellationToken cancellationToken = default)
    {
        var entities = await context.Stops
            .AsNoTracking()
            .OrderBy(s => s.Code)
            .ToListAsync(cancellationToken);

        return entities.Select(StopMapper.ToModel).ToList();
    }

    public async Task<IReadOnlyList<IRouteModel>> GetRoutesAsync(CancellationToken cancellationToken = default)
    {
        var entities = await context.Routes
            .AsNoTracking()
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Code)
            .ToListAsync(cancellationToken);

        return entities.Select(RouteMapper.ToModel).ToList();
    }

    public async Task<IReadOnlyList<IServiceCalendarModel>> GetServiceCalendarsAsync(CancellationToken cancellationToken = default)
    {
        var entities = await context.ServiceCalendars
            .AsNoTracking()
            .OrderBy(c => c.Code)
            .ToListAsync(cancellationToken);

        return entities.Select(ServiceCalendarMapper.ToModel).ToList();
    }

    public async Task<IReadOnlyList<ICalendarDateModel>> GetCalendarDatesAsync(CancellationToken cancellationToken = default)
    {
        var entities = await context.CalendarDates
            .AsNoTracking()
            .OrderBy(d => d.ServiceCalendarId)
            .ThenBy(d => d.Date)
            .ToListAsync(cancellationToken);

        return entities.Select(ServiceCalendarMapper.ToModel).ToList();
    }

    public async Task<IReadOnlyList<ITripModel>> GetTripsAsync(CancellationToken cancellationToken = default)
    {
        var entities = await context.Trips
            .AsNoTracking()
            .OrderBy(t => t.Code)
            .ToListAsync(cancellationToken);

        return entities.Select(TripMapper.ToModel).ToList();
    }

    public async Task<IReadOnlyList<IStopTimeModel>> GetStopTimesAsync(CancellationToken cancellationToken = default)
    {
        var entities = await context.StopTimes
            .AsNoTracking()
            .OrderBy(st => st.TripId)
            .ThenBy(st => st.StopSequence)
            .ToListAsync(cancellationToken);

        return entities.Select(TripMapper.ToModel).ToList();
    }
}
