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
        var errors = new ValidationErrors();
        if (limit is < 1 or > MaxLimit)
        {
            errors.Add("Limit", $"Broj polazaka mora biti između 1 i {MaxLimit}.");
        }
        errors.ThrowIfAny();

        var stop = await stopRepository.GetAsync(stopId, cancellationToken)
            ?? throw new NotFoundException($"Stanica {stopId} ne postoji.");

        // Vožnje staju na peronima, ne na kolodvoru.
        IReadOnlyList<Guid> stopIds = stop.LocationType == LocationType.Station
            ? (await departureRepository.GetPlatformsAsync(stop.Id, cancellationToken)).Select(p => p.Id).ToList()
            : [stop.Id];

        var timeZone = await GetTimeZoneAsync(cancellationToken);
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

        return new StopDepartures(stop, TimeZoneInfo.ConvertTime(now, timeZone), next);
    }

    /// <summary>Svi prijevoznici imaju istu zonu (GTFS), pa se uzima zona prvog.</summary>
    private async Task<TimeZoneInfo> GetTimeZoneAsync(CancellationToken cancellationToken)
    {
        var timezone = await departureRepository.GetTimezoneAsync(cancellationToken)
            ?? throw new UnprocessableException("Polasci se ne mogu izračunati.", ["Nije unesen nijedan prijevoznik."]);

        return TimeZoneInfo.FindSystemTimeZoneById(timezone);
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
