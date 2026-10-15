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

    /// <summary>Koliko se unaprijed traže polasci.</summary>
    private static readonly TimeSpan LookAhead = TimeSpan.FromHours(24);

    /// <summary>
    /// GTFS vremena idu do 48 h, pa polazak danas može pripadati danu voznog reda
    /// od jučer ili prekjučer.
    /// </summary>
    private const int ServiceDaysBack = 2;

    public async Task<StopDepartures> GetNextAsync(Guid stopId, DateTimeOffset now, int limit, CancellationToken cancellationToken = default)
    {
        ValidateLimit(limit);

        var stop = await stopRepository.GetAsync(stopId, cancellationToken)
            ?? throw new NotFoundException($"Stanica {stopId} ne postoji.");

        return await GetNextAsync(stop, now, limit, cancellationToken);
    }

    public async Task<StopDepartures> GetNextAsync(string stopCode, DateTimeOffset now, int limit, CancellationToken cancellationToken = default)
    {
        ValidateLimit(limit);

        var stop = await stopRepository.GetByCodeAsync(stopCode, cancellationToken)
            ?? throw new NotFoundException($"Stanica '{stopCode}' ne postoji.");

        return await GetNextAsync(stop, now, limit, cancellationToken);
    }

    private async Task<StopDepartures> GetNextAsync(IStopModel stop, DateTimeOffset now, int limit, CancellationToken cancellationToken)
    {
        // Vožnje staju na peronima, ne na kolodvoru.
        IReadOnlyList<Guid> stopIds = stop.LocationType == LocationType.Station
            ? (await departureRepository.GetPlatformsAsync(stop.Id, cancellationToken)).Select(p => p.Id).ToList()
            : [stop.Id];

        var agency = await departureRepository.GetPrimaryAgencyAsync(cancellationToken)
            ?? throw new UnprocessableException("Polasci se ne mogu izračunati.", ["Nije unesen nijedan prijevoznik."]);

        // Svi prijevoznici imaju istu zonu (GTFS; provjerava AgencyService).
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(agency.Timezone);
        var until = now + LookAhead;

        var firstDay = LocalDate(now, timeZone).AddDays(-ServiceDaysBack);
        var lastDay = LocalDate(until, timeZone);

        var calendars = await departureRepository.GetCalendarsAsync(cancellationToken);
        var calendarDates = await departureRepository.GetCalendarDatesAsync(firstDay, lastDay, cancellationToken);

        var departures = new List<Departure>();

        for (var day = firstDay; stopIds.Count > 0 && day <= lastDay; day = day.AddDays(1))
        {
            var calendarIds = ServiceCalendarRules.ActiveOn(calendars, calendarDates, day);
            if (calendarIds.Count == 0)
            {
                continue;
            }

            // Raspon GTFS vremena tog dana voznog reda koji pada u [now, until].
            var start = ServiceDay.Start(day, timeZone);
            var fromSeconds = Math.Max(0, (int)Math.Ceiling((now - start).TotalSeconds));
            var toSeconds = Math.Min(GtfsTime.MaxSeconds, (int)Math.Floor((until - start).TotalSeconds));
            if (fromSeconds > toSeconds)
            {
                continue;
            }

            // Svaki dan vraća najviše limit polazaka, pa ih spojeni popis sigurno sadrži prvih limit.
            var scheduled = await departureRepository.GetScheduledAsync(
                stopIds, calendarIds, fromSeconds, toSeconds, limit, cancellationToken);

            departures.AddRange(scheduled.Select(s => ToDeparture(s, day, timeZone)));
        }

        var next = departures
            .OrderBy(d => d.DepartureTime)
            .ThenBy(d => d.RouteShortName ?? d.RouteLongName)
            .Take(limit)
            .ToList();

        return new StopDepartures(stop, agency.Name, TimeZoneInfo.ConvertTime(now, timeZone), next);
    }

    private static void ValidateLimit(int limit)
    {
        var errors = new ValidationErrors();
        if (limit is < 1 or > MaxLimit)
        {
            errors.Add("Limit", $"Broj polazaka mora biti između 1 i {MaxLimit}.");
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
