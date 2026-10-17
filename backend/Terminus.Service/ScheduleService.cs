using Terminus.Common;
using Terminus.Common.Exceptions;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Repository.Common;
using Terminus.Service.Common;

namespace Terminus.Service;

public class ScheduleService(
    IRouteRepository routeRepository,
    ITimetableRepository timetableRepository,
    IScheduleRepository scheduleRepository,
    IStopRepository stopRepository,
    IDepartureRepository departureRepository,
    IShapeRepository shapeRepository) : IScheduleService
{
    /// <summary>Najviše vožnji u jednom spremanju; zaštita od pogrešnog klijenta.</summary>
    public const int MaxChanges = 5000;

    public async Task<RouteSchedule> GetAsync(Guid routeId, TripDirection? directionId, Guid? calendarId, CancellationToken cancellationToken = default)
    {
        if (directionId is { } direction && !Enum.IsDefined(direction))
        {
            var errors = new ValidationErrors();
            errors.Add("DirectionId", "Smjer mora biti 0 ili 1.");
            errors.ThrowIfAny();
        }

        var route = await routeRepository.GetAsync(routeId, cancellationToken)
            ?? throw new NotFoundException($"Linija {routeId} ne postoji.");

        // Redci se računaju iz svih vožnji smjera, pa se ne mijenjaju s tipom dana.
        var all = (await timetableRepository.GetRouteTripsAsync(routeId, directionId, null, cancellationToken))
            .Where(t => t.StopTimes.Count > 0)
            .ToList();

        var rows = StopSequenceMerger.Merge(all
            .Select(t => (IReadOnlyList<Guid>)t.StopTimes.Select(st => st.StopId).ToList())
            .DistinctBy(s => string.Join(',', s)));
        var stops = (await stopRepository.GetManyAsync(rows, cancellationToken)).ToDictionary(s => s.Id);

        var trips = all
            .Where(t => calendarId is null || t.Trip.ServiceCalendarId == calendarId)
            .OrderBy(t => t.StopTimes[0].DepartureTime)
            .ThenBy(t => t.Trip.Code, StringComparer.Ordinal)
            .ToList();

        return new RouteSchedule(
            route,
            directionId,
            rows.Where(stops.ContainsKey).Select(id => stops[id]).ToList(),
            trips,
            all.CountBy(t => t.Trip.ServiceCalendarId).ToDictionary());
    }

    public async Task SaveAsync(Guid routeId, ScheduleChanges changes, CancellationToken cancellationToken = default)
    {
        var route = await routeRepository.GetAsync(routeId, cancellationToken)
            ?? throw new NotFoundException($"Linija {routeId} ne postoji.");

        var errors = new ValidationErrors();
        if (changes.Upserts.Count + changes.Deletes.Count > MaxChanges)
        {
            errors.Add("Upserts", $"Najviše {MaxChanges} vožnji u jednom spremanju.");
            errors.ThrowIfAny();
        }
        if (changes.DirectionId is { } direction && !Enum.IsDefined(direction))
        {
            errors.Add("DirectionId", "Smjer mora biti 0 ili 1.");
        }

        // Postojeće vožnje moraju pripadati liniji i biti iste verzije kao kod klijenta.
        var known = changes.Upserts.Where(u => u.Trip.Id != Guid.Empty).Select(u => u.Trip.Id)
            .Concat(changes.Deletes.Select(d => d.Id))
            .ToList();
        var existing = await scheduleRepository.GetTripsAsync(known, cancellationToken);

        var stale = 0;
        foreach (var (id, updatedAt) in changes.Upserts
            .Where(u => u.Trip.Id != Guid.Empty)
            .Select(u => (u.Trip.Id, u.UpdatedAt))
            .Concat(changes.Deletes.Select(d => (d.Id, (DateTime?)d.UpdatedAt))))
        {
            if (!existing.TryGetValue(id, out var trip) || trip.RouteId != routeId)
            {
                stale++;
            }
            else if (updatedAt is null || !SameInstant(trip.UpdatedAt, updatedAt.Value))
            {
                stale++;
            }
        }
        if (stale > 0)
        {
            throw new ConflictException(
                $"{stale} vožnji je u međuvremenu promijenjeno ili obrisano. Učitaj vozni red ponovno pa ponovi izmjene.");
        }

        var calendars = (await departureRepository.GetCalendarsAsync(cancellationToken)).Select(c => c.Id).ToHashSet();
        var stopIds = changes.Upserts.SelectMany(u => u.StopTimes).Select(st => st.StopId).Distinct().ToList();
        var stops = (await stopRepository.GetManyAsync(stopIds, cancellationToken)).ToDictionary(s => s.Id);
        var shapes = new Dictionary<Guid, bool>();

        var upserts = new List<TripStopTimes>();
        for (var i = 0; i < changes.Upserts.Count; i++)
        {
            var (trip, stopTimes, _) = changes.Upserts[i];
            var path = $"Upserts[{i}]";

            if (trip.Id == Guid.Empty)
            {
                trip.Id = Guid.NewGuid();
                trip.Code = $"{route.Code}-{trip.Id.ToString("N")[..8]}";
            }
            else
            {
                // Kod postojeće vožnje se ne mijenja.
                trip.Code = existing[trip.Id].Code;
            }
            trip.RouteId = routeId;
            trip.DirectionId = changes.DirectionId;
            trip.Headsign = string.IsNullOrWhiteSpace(trip.Headsign) ? null : trip.Headsign.Trim();
            trip.ShortName = string.IsNullOrWhiteSpace(trip.ShortName) ? null : trip.ShortName.Trim();
            trip.BlockId = string.IsNullOrWhiteSpace(trip.BlockId) ? null : trip.BlockId.Trim();

            if (!calendars.Contains(trip.ServiceCalendarId))
            {
                errors.Add($"{path}.ServiceCalendarId", "Kalendar ne postoji.");
            }
            if (trip.ShapeId is { } shapeId)
            {
                if (!shapes.TryGetValue(shapeId, out var shapeExists))
                {
                    shapes[shapeId] = shapeExists = await shapeRepository.ExistsAsync(shapeId, cancellationToken);
                }
                if (!shapeExists)
                {
                    errors.Add($"{path}.ShapeId", "Trasa ne postoji.");
                }
            }
            if (!Enum.IsDefined(trip.WheelchairAccessible) || !Enum.IsDefined(trip.BikesAllowed))
            {
                errors.Add(path, "Nepoznata vrijednost pristupačnosti ili prijevoza bicikala.");
            }

            if (stopTimes.Count < StopTimeRules.MinStopTimes)
            {
                errors.Add($"{path}.StopTimes", $"Vožnja mora imati barem {StopTimeRules.MinStopTimes} stanice.");
            }
            else
            {
                for (var j = 0; j < stopTimes.Count; j++)
                {
                    stopTimes[j].TripId = trip.Id;
                    stopTimes[j].StopSequence = j + 1;
                    stopTimes[j].StopHeadsign = string.IsNullOrWhiteSpace(stopTimes[j].StopHeadsign) ? null : stopTimes[j].StopHeadsign!.Trim();
                }
                StopTimeRules.Validate(stopTimes, stops, errors, $"{path}.StopTimes");
            }

            upserts.Add(new TripStopTimes(trip, stopTimes));
        }
        errors.ThrowIfAny();

        await scheduleRepository.SaveAsync(upserts, changes.Deletes.Select(d => d.Id).ToList(), cancellationToken);
    }

    /// <summary>Baza čuva mikrosekunde, pa se uspoređuje na mikrosekundu.</summary>
    private static bool SameInstant(DateTime a, DateTime b) =>
        Math.Abs((a.ToUniversalTime() - b.ToUniversalTime()).Ticks) < TimeSpan.TicksPerMillisecond / 1000;
}
