using Terminus.Common;
using Terminus.Common.Exceptions;
using Terminus.Model;
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
    IShapeRepository shapeRepository,
    IRouteVariantRepository variantRepository) : IScheduleService
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

        // Redci se računaju iz varijanti i svih vožnji smjera, pa se ne mijenjaju s
        // tipom dana. Varijante idu prve (glavna prva), da redoslijed prati njih.
        var all = (await timetableRepository.GetRouteTripsAsync(routeId, directionId, null, cancellationToken))
            .Where(t => t.StopTimes.Count > 0)
            .ToList();
        var variants = (await variantRepository.ListByRouteAsync(routeId, cancellationToken))
            .Where(v => directionId is null || v.DirectionId == directionId)
            .ToList();

        var rows = StopSequenceMerger.Merge(variants
            .Select(v => (IReadOnlyList<Guid>)v.Stops.Select(s => s.StopId).ToList())
            .Concat(all.Select(t => (IReadOnlyList<Guid>)t.StopTimes.Select(st => st.StopId).ToList()))
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
            variants,
            trips,
            all.CountBy(t => t.Trip.ServiceCalendarId).ToDictionary(),
            all.Where(t => t.Trip.VariantId is not null).CountBy(t => t.Trip.VariantId!.Value).ToDictionary(),
            all.Where(t => t.Trip.DepartureId is not null)
                .GroupBy(t => t.Trip.DepartureId!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<Guid>)g.Select(t => t.Trip.ServiceCalendarId).Distinct().ToList()));
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
        var variants = (await variantRepository.ListByRouteAsync(routeId, cancellationToken)).Select(v => v.Id).ToHashSet();

        var upserts = new List<TripStopTimes>();
        for (var i = 0; i < changes.Upserts.Count; i++)
        {
            var (trip, stopTimes, _, _, _) = changes.Upserts[i];
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
            if (trip.VariantId is { } variantId && !variants.Contains(variantId))
            {
                errors.Add($"{path}.VariantId", "Varijanta ne pripada ovoj liniji.");
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
                    // Okvirna linija: točni su samo polazak i dolazak na terminus (GTFS timepoint).
                    stopTimes[j].Timepoint = route.TimeMode == TimeMode.Exact || j == 0 || j == stopTimes.Count - 1;
                }
                StopTimeRules.Validate(stopTimes, stops, errors, $"{path}.StopTimes");
            }

            upserts.Add(new TripStopTimes(trip, stopTimes));

            if (changes.Upserts[i].DayTypes is { } dayTypes && dayTypes.Any(c => !calendars.Contains(c)))
            {
                errors.Add($"{path}.DayTypes", "Tip dana ne postoji.");
            }
        }
        errors.ThrowIfAny();

        var deletes = changes.Deletes.Select(d => d.Id).ToList();
        var (copies, removed) = await SyncDeparturesAsync(route, changes.Upserts, upserts, existing, deletes, cancellationToken);

        await scheduleRepository.SaveAsync([.. upserts, .. copies], [.. deletes, .. removed], cancellationToken);
    }

    /// <summary>
    /// Polasci: izmijenjena vožnja prenosi vremena na vožnje istog polaska u ostalim
    /// tipovima dana, a prema traženim tipovima dana takve vožnje nastaju ili se brišu.
    /// Vožnje koje su same u izmjenama ili brisanjima se ne diraju.
    /// </summary>
    /// <returns>Vožnje ostalih tipova dana za spremanje i Id-evi vožnji za brisanje.</returns>
    private async Task<(List<TripStopTimes> Copies, List<Guid> Removed)> SyncDeparturesAsync(
        IRouteModel route,
        IReadOnlyList<ScheduleTripChange> changes,
        IReadOnlyList<TripStopTimes> upserts,
        IReadOnlyDictionary<Guid, ITripModel> existing,
        IReadOnlyList<Guid> deletes,
        CancellationToken cancellationToken)
    {
        Guid? DepartureOf(ITripModel trip) => existing.TryGetValue(trip.Id, out var old) ? old.DepartureId : null;

        var departureIds = upserts.Select(u => DepartureOf(u.Trip)).OfType<Guid>().Distinct().ToList();
        var siblingsByDeparture = (await scheduleRepository.GetDepartureTripsAsync(departureIds, cancellationToken))
            .GroupBy(t => t.Trip.DepartureId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());
        var handled = upserts.Select(u => u.Trip.Id).Concat(deletes).ToHashSet();

        var copies = new List<TripStopTimes>();
        var removed = new List<Guid>();
        for (var i = 0; i < upserts.Count; i++)
        {
            var (trip, stopTimes) = upserts[i];
            var departureId = DepartureOf(trip);
            if (changes[i].Detach)
            {
                trip.DepartureId = null;
                continue;
            }

            var siblings = departureId is { } id && siblingsByDeparture.TryGetValue(id, out var group)
                ? group.Where(s => s.Trip.Id != trip.Id).ToList()
                : [];
            var dayTypes = (changes[i].DayTypes ?? siblings.Select(s => s.Trip.ServiceCalendarId))
                .Append(trip.ServiceCalendarId)
                .ToHashSet();

            trip.DepartureId = dayTypes.Count > 1 ? departureId ?? Guid.NewGuid() : null;
            foreach (var sibling in siblings.Where(s => !handled.Contains(s.Trip.Id)))
            {
                // Vožnja premještena u tip dana u kojem polazak već vozi zamjenjuje tamošnju.
                if (sibling.Trip.ServiceCalendarId == trip.ServiceCalendarId)
                {
                    removed.Add(sibling.Trip.Id);
                }
                else if (dayTypes.Contains(sibling.Trip.ServiceCalendarId))
                {
                    // Vozilo (BlockId) može biti drukčije po tipu dana, pa ostaje.
                    copies.Add(Copy(trip, stopTimes, sibling.Trip.Id, sibling.Trip.Code, sibling.Trip.ServiceCalendarId, sibling.Trip.BlockId));
                }
                else
                {
                    removed.Add(sibling.Trip.Id);
                }
            }
            foreach (var calendar in dayTypes.Where(c => c != trip.ServiceCalendarId && siblings.All(s => s.Trip.ServiceCalendarId != c)))
            {
                var copyId = Guid.NewGuid();
                copies.Add(Copy(trip, stopTimes, copyId, $"{route.Code}-{copyId.ToString("N")[..8]}", calendar, null));
            }
        }
        return (copies, removed);
    }

    /// <summary>Ista vožnja (vremena, varijanta, trasa) u drugom tipu dana.</summary>
    private static TripStopTimes Copy(
        ITripModel source,
        IReadOnlyList<IStopTimeModel> stopTimes,
        Guid id,
        string code,
        Guid calendarId,
        string? blockId) =>
        new(
            new TripModel
            {
                Id = id,
                Code = code,
                RouteId = source.RouteId,
                ServiceCalendarId = calendarId,
                Headsign = source.Headsign,
                ShortName = source.ShortName,
                DirectionId = source.DirectionId,
                BlockId = blockId,
                ShapeId = source.ShapeId,
                VariantId = source.VariantId,
                DepartureId = source.DepartureId,
                WheelchairAccessible = source.WheelchairAccessible,
                BikesAllowed = source.BikesAllowed
            },
            stopTimes.Select(st => (IStopTimeModel)new StopTimeModel
            {
                TripId = id,
                StopSequence = st.StopSequence,
                StopId = st.StopId,
                ArrivalTime = st.ArrivalTime,
                DepartureTime = st.DepartureTime,
                StopHeadsign = st.StopHeadsign,
                PickupType = st.PickupType,
                DropOffType = st.DropOffType,
                ContinuousPickup = st.ContinuousPickup,
                ContinuousDropOff = st.ContinuousDropOff,
                ShapeDistTraveled = st.ShapeDistTraveled,
                Timepoint = st.Timepoint
            }).ToList());

    /// <summary>Baza čuva mikrosekunde, pa se uspoređuje na mikrosekundu.</summary>
    private static bool SameInstant(DateTime a, DateTime b) =>
        Math.Abs((a.ToUniversalTime() - b.ToUniversalTime()).Ticks) < TimeSpan.TicksPerMillisecond / 1000;
}
