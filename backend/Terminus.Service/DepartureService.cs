using Terminus.Common;
using Terminus.Common.Exceptions;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Repository.Common;
using Terminus.Service.Common;

namespace Terminus.Service;

public class DepartureService(IDepartureRepository departureRepository, IStopRepository stopRepository) : IDepartureService
{
    public const int MaxLimit = 30;

    /// <summary>Najviše upravo otišlih polazaka uz sljedeće.</summary>
    public const int MaxPast = 5;

    /// <summary>Koliko unatrag polazak još vrijedi kao "upravo otišao".</summary>
    private static readonly TimeSpan LookBack = TimeSpan.FromMinutes(30);

    /// <summary>Gornja granica polazaka u jednom danu, zaštita od pogrešnih podataka.</summary>
    public const int MaxDayDepartures = 5000;

    /// <summary>Koliko se unaprijed traže polasci.</summary>
    private static readonly TimeSpan LookAhead = TimeSpan.FromHours(24);

    /// <summary>
    /// GTFS vremena idu do 48 h, pa polazak danas može pripadati danu voznog reda
    /// od jučer ili prekjučer.
    /// </summary>
    private const int ServiceDaysBack = 2;

    public async Task<StopDepartures> GetNextAsync(Guid stopId, DateTimeOffset now, int limit, int past, CancellationToken cancellationToken = default)
    {
        ValidateLimit(limit, past);

        var stop = await stopRepository.GetAsync(stopId, cancellationToken)
            ?? throw new NotFoundException($"Stanica {stopId} ne postoji.");

        return await GetNextAsync(stop, now, limit, past, cancellationToken);
    }

    public async Task<StopDepartures> GetNextAsync(string stopCode, DateTimeOffset now, int limit, int past, CancellationToken cancellationToken = default)
    {
        ValidateLimit(limit, past);

        var stop = await stopRepository.GetByCodeAsync(stopCode, cancellationToken)
            ?? throw new NotFoundException($"Stanica '{stopCode}' ne postoji.");

        return await GetNextAsync(stop, now, limit, past, cancellationToken);
    }

    public async Task<StopDayDepartures> GetForDayAsync(Guid stopId, DateOnly? date, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var stop = await stopRepository.GetAsync(stopId, cancellationToken)
            ?? throw new NotFoundException($"Stanica {stopId} ne postoji.");

        var stopIds = await GetServedStopIdsAsync(stop, cancellationToken);
        var timeZone = await GetTimeZoneAsync(cancellationToken);
        var day = date ?? LocalDate(now, timeZone);

        var calendars = await departureRepository.GetCalendarsAsync(cancellationToken);
        var calendarDates = await departureRepository.GetCalendarDatesAsync(day, day, cancellationToken);
        var calendarIds = ServiceCalendarRules.ActiveOn(calendars, calendarDates, day);

        var scheduled = stopIds.Count == 0 || calendarIds.Count == 0
            ? []
            : await departureRepository.GetScheduledAsync(
                stopIds, calendarIds, 0, GtfsTime.MaxSeconds, MaxDayDepartures, cancellationToken);

        var departures = scheduled
            .Select(s => ToDeparture(s, day, timeZone))
            .OrderBy(d => d.DepartureTime)
            .ThenBy(d => d.RouteShortName ?? d.RouteLongName)
            .ToList();

        return new StopDayDepartures(stop, day, departures);
    }

    private async Task<StopDepartures> GetNextAsync(IStopModel stop, DateTimeOffset now, int limit, int past, CancellationToken cancellationToken)
    {
        var stopIds = await GetServedStopIdsAsync(stop, cancellationToken);

        var agency = await departureRepository.GetPrimaryAgencyAsync(cancellationToken)
            ?? throw new UnprocessableException("Polasci se ne mogu izračunati.", ["Nije unesen nijedan prijevoznik."]);

        // Svi prijevoznici imaju istu zonu (GTFS; provjerava AgencyService).
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(agency.Timezone);
        var until = now + LookAhead;
        var since = now - LookBack;

        var firstDay = LocalDate(since, timeZone).AddDays(-ServiceDaysBack);
        var lastDay = LocalDate(until, timeZone);

        var calendars = await departureRepository.GetCalendarsAsync(cancellationToken);
        var calendarDates = await departureRepository.GetCalendarDatesAsync(firstDay, lastDay, cancellationToken);

        // Polasci od from do to (bez samog to ako je beforeTo) iz svih dana voznog reda, po vremenu.
        async Task<List<Departure>> BetweenAsync(DateTimeOffset from, DateTimeOffset to, int perDay, bool beforeTo)
        {
            var found = new List<Departure>();
            for (var day = firstDay; stopIds.Count > 0 && day <= lastDay; day = day.AddDays(1))
            {
                var calendarIds = ServiceCalendarRules.ActiveOn(calendars, calendarDates, day);
                if (calendarIds.Count == 0)
                {
                    continue;
                }

                // Raspon GTFS vremena tog dana voznog reda koji pada u [from, to].
                var start = ServiceDay.Start(day, timeZone);
                var fromSeconds = Math.Max(0, (int)Math.Ceiling((from - start).TotalSeconds));
                var toSeconds = Math.Min(GtfsTime.MaxSeconds, beforeTo
                    ? (int)Math.Ceiling((to - start).TotalSeconds) - 1
                    : (int)Math.Floor((to - start).TotalSeconds));
                if (fromSeconds > toSeconds)
                {
                    continue;
                }

                var scheduled = await departureRepository.GetScheduledAsync(
                    stopIds, calendarIds, fromSeconds, toSeconds, perDay, cancellationToken);

                found.AddRange(scheduled.Select(s => ToDeparture(s, day, timeZone)));
            }
            return found
                .OrderBy(d => d.DepartureTime)
                .ThenBy(d => d.RouteShortName ?? d.RouteLongName)
                .ToList();
        }

        // Svaki dan vraća najviše limit polazaka, pa ih spojeni popis sigurno sadrži prvih limit.
        var next = (await BetweenAsync(now, until, limit, beforeTo: false)).Take(limit).ToList();

        // Otišli polasci iz zadnjih pola sata; na vrlo prometnoj stanici iz prvih MaxLimit po danu.
        var recent = past == 0
            ? []
            : (await BetweenAsync(since, now, MaxLimit, beforeTo: true)).TakeLast(past).ToList();

        return new StopDepartures(stop, agency.Name, TimeZoneInfo.ConvertTime(now, timeZone), next, recent);
    }

    /// <summary>Vožnje staju na peronima, ne na kolodvoru.</summary>
    private async Task<IReadOnlyList<Guid>> GetServedStopIdsAsync(IStopModel stop, CancellationToken cancellationToken) =>
        stop.LocationType == LocationType.Station
            ? (await departureRepository.GetPlatformsAsync(stop.Id, cancellationToken)).Select(p => p.Id).ToList()
            : [stop.Id];

    /// <summary>Svi prijevoznici imaju istu zonu (GTFS; provjerava AgencyService).</summary>
    private async Task<TimeZoneInfo> GetTimeZoneAsync(CancellationToken cancellationToken)
    {
        var agency = await departureRepository.GetPrimaryAgencyAsync(cancellationToken)
            ?? throw new UnprocessableException("Polasci se ne mogu izračunati.", ["Nije unesen nijedan prijevoznik."]);

        return TimeZoneInfo.FindSystemTimeZoneById(agency.Timezone);
    }

    private static void ValidateLimit(int limit, int past)
    {
        var errors = new ValidationErrors();
        if (limit is < 1 or > MaxLimit)
        {
            errors.Add("Limit", $"Broj polazaka mora biti između 1 i {MaxLimit}.");
        }
        if (past is < 0 or > MaxPast)
        {
            errors.Add("Past", $"Broj otišlih polazaka mora biti između 0 i {MaxPast}.");
        }
        errors.ThrowIfAny();
    }

    private static DateOnly LocalDate(DateTimeOffset instant, TimeZoneInfo timeZone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, timeZone).DateTime);

    private static Departure ToDeparture(ScheduledDeparture s, DateOnly serviceDate, TimeZoneInfo timeZone) => new(
        s.TripId,
        s.RouteId,
        s.RouteShortName,
        s.RouteLongName,
        s.RouteColor,
        s.RouteTextColor,
        s.Headsign,
        s.StopId,
        s.PlatformCode,
        serviceDate,
        ServiceDay.ToDateTimeOffset(serviceDate, s.DepartureTime, timeZone));
}
