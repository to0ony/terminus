using Terminus.Common.Exceptions;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Repository.Common;
using Terminus.Service.Common;

namespace Terminus.Service;

public class TimetableService(
    ITimetableRepository timetableRepository,
    IRouteRepository routeRepository,
    IStopRepository stopRepository,
    IDepartureRepository departureRepository) : ITimetableService
{
    public async Task<IReadOnlyList<RoutePattern>> GetRoutePatternsAsync(Guid routeId, CancellationToken cancellationToken = default)
    {
        _ = await routeRepository.GetAsync(routeId, cancellationToken) ?? throw RouteNotFound(routeId);

        var trips = await timetableRepository.GetRouteTripsAsync(routeId, null, null, cancellationToken);

        var groups = trips
            .Where(t => t.StopTimes.Count > 0)
            .GroupBy(t => (t.Trip.DirectionId, Key: string.Join(',', t.StopTimes.Select(st => st.StopId))))
            .Select(g => new
            {
                g.Key.DirectionId,
                StopIds = g.First().StopTimes.Select(st => st.StopId).ToList(),
                Trips = g.Select(t => t.Trip).ToList()
            })
            .OrderBy(g => g.DirectionId is null)
            .ThenBy(g => g.DirectionId)
            .ThenByDescending(g => g.Trips.Count)
            .ThenByDescending(g => g.StopIds.Count)
            .ToList();

        var stops = await GetStopsAsync(groups.SelectMany(g => g.StopIds), cancellationToken);

        return groups
            .Select(g => new RoutePattern(
                g.DirectionId,
                MostCommon(g.Trips.Select(t => t.Headsign)),
                MostCommon(g.Trips.Select(t => t.ShapeId)),
                g.Trips.Count,
                g.StopIds.Select(id => stops[id]).ToList()))
            .ToList();
    }

    public async Task<RouteTimetable> GetRouteTimetableAsync(
        Guid routeId,
        RouteTimetableQuery query,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var errors = new ValidationErrors();
        if (query.Date is not null && query.ServiceCalendarId is not null)
        {
            errors.Add(nameof(query.Date), "Zadaje se datum ili kalendar, ne oboje.");
        }
        if (query.DirectionId is { } direction && !Enum.IsDefined(direction))
        {
            errors.Add(nameof(query.DirectionId), "Smjer mora biti 0 ili 1.");
        }
        errors.ThrowIfAny();

        var route = await routeRepository.GetAsync(routeId, cancellationToken) ?? throw RouteNotFound(routeId);
        var calendars = await departureRepository.GetCalendarsAsync(cancellationToken);

        DateOnly? date = null;
        IReadOnlyCollection<Guid> calendarIds;

        if (query.ServiceCalendarId is Guid calendarId)
        {
            if (calendars.All(c => c.Id != calendarId))
            {
                throw new NotFoundException($"Kalendar {calendarId} ne postoji.");
            }
            calendarIds = [calendarId];
        }
        else
        {
            date = query.Date ?? await TodayAsync(now, cancellationToken);
            var calendarDates = await departureRepository.GetCalendarDatesAsync(date.Value, date.Value, cancellationToken);
            calendarIds = ServiceCalendarRules.ActiveOn(calendars, calendarDates, date.Value);
        }

        var routeTrips = calendarIds.Count == 0
            ? []
            : await timetableRepository.GetRouteTripsAsync(routeId, query.DirectionId, calendarIds, cancellationToken);
        var stops = await GetStopsAsync(routeTrips.SelectMany(t => t.StopTimes).Select(st => st.StopId), cancellationToken);

        // Vožnje preskaču neaktivne stanice; ostale stanice zadržavaju svoja vremena.
        var trips = routeTrips
            .Select(t => t with { StopTimes = t.StopTimes.Where(st => stops.GetValueOrDefault(st.StopId)?.IsActive != false).ToList() })
            .Where(t => t.StopTimes.Count > 0)
            .OrderBy(t => t.StopTimes[0].DepartureTime)
            .ThenBy(t => t.Trip.Code, StringComparer.Ordinal)
            .ToList();

        var sequences = trips
            .Select(t => (IReadOnlyList<Guid>)t.StopTimes.Select(st => st.StopId).ToList())
            .DistinctBy(s => string.Join(',', s))
            .ToList();

        var rows = StopSequenceMerger.Merge(sequences);

        var timetableTrips = trips
            .Select(t =>
            {
                var times = new IStopTimeModel?[rows.Count];
                var rowIndexes = StopSequenceMerger.Map(rows, t.StopTimes.Select(st => st.StopId).ToList());
                for (var i = 0; i < rowIndexes.Length; i++)
                {
                    times[rowIndexes[i]] = t.StopTimes[i];
                }
                return new TimetableTrip(t.Trip, times);
            })
            .ToList();

        var usedCalendarIds = trips.Select(t => t.Trip.ServiceCalendarId).ToHashSet();

        return new RouteTimetable(
            route,
            date,
            query.ServiceCalendarId,
            query.DirectionId,
            rows.Select(id => stops[id]).ToList(),
            calendars.Where(c => usedCalendarIds.Contains(c.Id)).OrderBy(c => c.Code, StringComparer.Ordinal).ToList(),
            timetableTrips);
    }

    public async Task<IReadOnlyList<IRouteModel>> GetStopRoutesAsync(Guid stopId, CancellationToken cancellationToken = default)
    {
        var stop = await stopRepository.GetAsync(stopId, cancellationToken)
            ?? throw new NotFoundException($"Stanica {stopId} ne postoji.");

        // Vožnje staju na peronima, ne na kolodvoru.
        IReadOnlyCollection<Guid> stopIds = stop.LocationType == LocationType.Station
            ? (await departureRepository.GetPlatformsAsync(stop.Id, cancellationToken)).Select(p => p.Id).ToList()
            : [stop.Id];

        return stopIds.Count == 0
            ? []
            : await timetableRepository.GetRoutesServingAsync(stopIds, cancellationToken);
    }

    public async Task<ServiceOverview> GetServiceOverviewAsync(
        DateOnly? date,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var day = date ?? await TodayAsync(now, cancellationToken);
        var calendars = await departureRepository.GetCalendarsAsync(cancellationToken);
        var exceptions = (await departureRepository.GetCalendarDatesAsync(day, DateOnly.MaxValue, cancellationToken))
            .ToLookup(e => e.Date);

        bool RunsOn(IServiceCalendarModel calendar, DateOnly d) =>
            ServiceCalendarRules.RunsOn(calendar, exceptions[d], d);

        var active = calendars
            .Where(c => RunsOn(c, day))
            .OrderBy(c => c.Code, StringComparer.Ordinal)
            .ToList();

        // Zadnji mogući dan je kraj razdoblja kalendara s danima u tjednu ili
        // najkasniji dodani datum; od njega se ide unatrag do dana koji vozi
        // (ukinuti datumi na kraju razdoblja ga pomiču ranije).
        var latest = calendars
            .Where(HasWeekdays)
            .Select(c => c.EndDate)
            .Concat(exceptions.SelectMany(g => g).Where(e => e.ExceptionType == CalendarExceptionType.Added).Select(e => e.Date))
            .DefaultIfEmpty(DateOnly.MinValue)
            .Max();

        DateOnly? lastServiceDate = null;
        for (var d = latest; d >= day; d = d.AddDays(-1))
        {
            if (calendars.Any(c => RunsOn(c, d)))
            {
                lastServiceDate = d;
                break;
            }
        }

        return new ServiceOverview(day, active, lastServiceDate);
    }

    private static bool HasWeekdays(IServiceCalendarModel calendar) =>
        Enum.GetValues<DayOfWeek>().Any(d => ServiceCalendarRules.RunsOnWeekday(calendar, d));

    private async Task<DateOnly> TodayAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var agency = await departureRepository.GetPrimaryAgencyAsync(cancellationToken)
            ?? throw new UnprocessableException("Vozni red se ne može prikazati.", ["Nije unesen nijedan prijevoznik."]);

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(agency.Timezone);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, timeZone).DateTime);
    }

    private async Task<Dictionary<Guid, IStopModel>> GetStopsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        var distinct = ids.ToHashSet();
        if (distinct.Count == 0)
        {
            return [];
        }

        var stops = await stopRepository.GetManyAsync(distinct, cancellationToken);
        return stops.ToDictionary(s => s.Id);
    }

    private static T? MostCommon<T>(IEnumerable<T?> values) =>
        values
            .Where(v => v is not null)
            .GroupBy(v => v)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();

    private static NotFoundException RouteNotFound(Guid id) => new($"Linija {id} ne postoji.");
}
