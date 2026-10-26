using Terminus.Common.Exceptions;
using Terminus.Model.Common;
using Terminus.Repository.Common;
using Terminus.Service.Common;

namespace Terminus.Service;

public class LiveMapService(
    ITimetableRepository timetableRepository,
    IRouteRepository routeRepository,
    IRouteVariantRepository variantRepository,
    IStopRepository stopRepository,
    IShapeRepository shapeRepository,
    IDepartureRepository departureRepository) : ILiveMapService
{
    private const int Day = 24 * 60 * 60;

    public async Task<LiveRouteDay> GetRouteDayAsync(
        Guid routeId,
        DateOnly? date,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var route = await routeRepository.GetAsync(routeId, cancellationToken)
            ?? throw new NotFoundException($"Linija {routeId} ne postoji.");
        var agency = await departureRepository.GetPrimaryAgencyAsync(cancellationToken)
            ?? throw new UnprocessableException("Karta se ne može prikazati.", ["Nije unesen nijedan prijevoznik."]);

        var day = date ?? DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(now, TimeZoneInfo.FindSystemTimeZoneById(agency.Timezone)).DateTime);
        var previous = day.AddDays(-1);

        var calendars = await departureRepository.GetCalendarsAsync(cancellationToken);
        var exceptions = await departureRepository.GetCalendarDatesAsync(previous, day, cancellationToken);
        var today = ServiceCalendarRules.ActiveOn(calendars, exceptions, day);
        var yesterday = ServiceCalendarRules.ActiveOn(calendars, exceptions, previous);

        var routeTrips = today.Count + yesterday.Count == 0
            ? []
            : await timetableRepository.GetRouteTripsAsync(routeId, null, today.Union(yesterday).ToList(), cancellationToken);

        var stops = (await stopRepository.GetManyAsync(
                routeTrips.SelectMany(t => t.StopTimes).Select(st => st.StopId).ToHashSet(), cancellationToken))
            .Where(s => s.Latitude is not null && s.Longitude is not null)
            .ToDictionary(s => s.Id);

        // Vožnja bez trase uzima trasu svoje varijante.
        var variantShapes = (await variantRepository.ListByRouteAsync(routeId, cancellationToken))
            .ToDictionary(v => v.Id, v => v.ShapeId);

        LiveTrip? Trip(TripStopTimes trip, bool previousDay)
        {
            var offset = previousDay ? -Day : 0;
            var stopTimes = trip.StopTimes
                .Where(st => stops.ContainsKey(st.StopId))
                .Select(st => new LiveStopTime(st.StopId, st.ArrivalTime + offset, st.DepartureTime + offset))
                .ToList();

            // Od prethodnog dana samo vožnje koje traju iza ponoći.
            if (stopTimes.Count < 2 || previousDay && stopTimes[^1].ArrivalTime <= 0)
            {
                return null;
            }

            var shapeId = trip.Trip.ShapeId
                ?? (trip.Trip.VariantId is { } variantId ? variantShapes.GetValueOrDefault(variantId) : null);
            var headsign = string.IsNullOrWhiteSpace(trip.Trip.Headsign)
                ? stops[stopTimes[^1].StopId].Name
                : trip.Trip.Headsign.Trim();
            return new LiveTrip(trip.Trip.Id, trip.Trip.ShortName, headsign, trip.Trip.DirectionId, shapeId, previousDay, stopTimes);
        }

        var trips = routeTrips
            .SelectMany(t => new[]
            {
                today.Contains(t.Trip.ServiceCalendarId) ? Trip(t, previousDay: false) : null,
                yesterday.Contains(t.Trip.ServiceCalendarId) ? Trip(t, previousDay: true) : null
            })
            .OfType<LiveTrip>()
            .OrderBy(t => t.StopTimes[0].DepartureTime)
            .ToList();

        var shapes = new List<IShapeModel>();
        foreach (var shapeId in trips.Select(t => t.ShapeId).OfType<Guid>().Distinct())
        {
            if (await shapeRepository.GetAsync(shapeId, cancellationToken) is { Points.Count: >= 2 } shape)
            {
                shapes.Add(shape);
            }
        }

        var usedStops = trips.SelectMany(t => t.StopTimes).Select(st => st.StopId).ToHashSet();
        return new LiveRouteDay(
            route,
            day,
            agency.Timezone,
            trips,
            shapes,
            stops.Values.Where(s => usedStops.Contains(s.Id)).ToList());
    }
}
