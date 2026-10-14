using Microsoft.EntityFrameworkCore;
using Terminus.DAL;
using Terminus.Model.Common;
using Terminus.Repository.Common;
using Terminus.Repository.Mapping;

namespace Terminus.Repository;

public class GtfsExportRepository(TerminusDbContext context) : IGtfsExportRepository
{
    public async Task<IAgencyModel?> GetAgencyAsync(Guid agencyId, CancellationToken cancellationToken = default)
    {
        var entity = await context.Agencies
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == agencyId, cancellationToken);

        return entity is null ? null : AgencyMapper.ToModel(entity);
    }

    public async Task<IReadOnlyList<IStopModel>> GetStopsAsync(Guid agencyId, CancellationToken cancellationToken = default)
    {
        var entities = await context.Stops
            .AsNoTracking()
            .Where(s => s.AgencyId == agencyId)
            .OrderBy(s => s.Code)
            .ToListAsync(cancellationToken);

        return entities.Select(StopMapper.ToModel).ToList();
    }

    public async Task<IReadOnlyList<IRouteModel>> GetRoutesAsync(Guid agencyId, CancellationToken cancellationToken = default)
    {
        var entities = await context.Routes
            .AsNoTracking()
            .Where(r => r.AgencyId == agencyId)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Code)
            .ToListAsync(cancellationToken);

        return entities.Select(RouteMapper.ToModel).ToList();
    }

    public async Task<IReadOnlyList<IServiceCalendarModel>> GetServiceCalendarsAsync(Guid agencyId, CancellationToken cancellationToken = default)
    {
        var entities = await context.ServiceCalendars
            .AsNoTracking()
            .Where(c => c.AgencyId == agencyId)
            .OrderBy(c => c.Code)
            .ToListAsync(cancellationToken);

        return entities.Select(ServiceCalendarMapper.ToModel).ToList();
    }

    public async Task<IReadOnlyList<ICalendarDateModel>> GetCalendarDatesAsync(Guid agencyId, CancellationToken cancellationToken = default)
    {
        var entities = await context.CalendarDates
            .AsNoTracking()
            .Where(d => d.ServiceCalendar.AgencyId == agencyId)
            .OrderBy(d => d.ServiceCalendarId)
            .ThenBy(d => d.Date)
            .ToListAsync(cancellationToken);

        return entities.Select(ServiceCalendarMapper.ToModel).ToList();
    }

    public async Task<IReadOnlyList<ITripModel>> GetTripsAsync(Guid agencyId, CancellationToken cancellationToken = default)
    {
        var entities = await context.Trips
            .AsNoTracking()
            .Where(t => t.AgencyId == agencyId)
            .OrderBy(t => t.Code)
            .ToListAsync(cancellationToken);

        return entities.Select(TripMapper.ToModel).ToList();
    }

    public async Task<IReadOnlyList<IStopTimeModel>> GetStopTimesAsync(Guid agencyId, CancellationToken cancellationToken = default)
    {
        var entities = await context.StopTimes
            .AsNoTracking()
            .Where(st => st.Trip.AgencyId == agencyId)
            .OrderBy(st => st.TripId)
            .ThenBy(st => st.StopSequence)
            .ToListAsync(cancellationToken);

        return entities.Select(TripMapper.ToModel).ToList();
    }
}
