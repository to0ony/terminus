using Terminus.Common;
using Terminus.Common.Exceptions;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Model.Common.Filters;
using Terminus.Repository.Common;
using Terminus.Service.Common;

namespace Terminus.Service;

public class TripService(
    ITripRepository tripRepository,
    IStopTimeRepository stopTimeRepository,
    IRouteRepository routeRepository,
    IServiceCalendarRepository calendarRepository,
    IStopRepository stopRepository) : ITripService
{
    /// <summary>GTFS: vožnja mora imati barem dvije stanice.</summary>
    private const int MinStopTimes = 2;

    public async Task<ITripModel> GetAsync(Guid agencyId, Guid id, CancellationToken cancellationToken = default) =>
        await tripRepository.GetAsync(agencyId, id, cancellationToken) ?? throw TripNotFound(id);

    public Task<PagedResult<ITripModel>> FindAsync(Guid agencyId, TripFilter filter, CancellationToken cancellationToken = default)
    {
        var errors = new ValidationErrors();
        errors.AddPagingErrors(filter.Page, filter.PageSize);
        errors.ThrowIfAny();

        return tripRepository.FindAsync(agencyId, filter, cancellationToken);
    }

    public async Task<ITripModel> CreateAsync(Guid agencyId, ITripModel trip, CancellationToken cancellationToken = default)
    {
        trip.Id = Guid.Empty;
        trip.AgencyId = agencyId;
        Normalize(trip);
        await ValidateAsync(trip, cancellationToken);

        return await tripRepository.InsertAsync(trip, cancellationToken);
    }

    public async Task<ITripModel> UpdateAsync(Guid agencyId, Guid id, ITripModel trip, CancellationToken cancellationToken = default)
    {
        trip.Id = id;
        trip.AgencyId = agencyId;
        Normalize(trip);
        await ValidateAsync(trip, cancellationToken);

        return await tripRepository.UpdateAsync(trip, cancellationToken) ?? throw TripNotFound(id);
    }

    public async Task DeleteAsync(Guid agencyId, Guid id, CancellationToken cancellationToken = default)
    {
        if (!await tripRepository.DeleteAsync(agencyId, id, cancellationToken))
        {
            throw TripNotFound(id);
        }
    }

    public async Task<IReadOnlyList<IStopTimeModel>> GetStopTimesAsync(Guid agencyId, Guid tripId, CancellationToken cancellationToken = default)
    {
        await EnsureTripExistsAsync(agencyId, tripId, cancellationToken);
        return await stopTimeRepository.GetByTripAsync(tripId, cancellationToken);
    }

    public async Task<IReadOnlyList<IStopTimeModel>> ReplaceStopTimesAsync(
        Guid agencyId,
        Guid tripId,
        IReadOnlyList<IStopTimeModel> stopTimes,
        CancellationToken cancellationToken = default)
    {
        await EnsureTripExistsAsync(agencyId, tripId, cancellationToken);

        for (var i = 0; i < stopTimes.Count; i++)
        {
            stopTimes[i].TripId = tripId;
            stopTimes[i].StopSequence = i + 1;
            stopTimes[i].StopHeadsign = NullIfBlank(stopTimes[i].StopHeadsign);
        }

        await ValidateStopTimesAsync(agencyId, stopTimes, cancellationToken);

        return await stopTimeRepository.ReplaceForTripAsync(tripId, stopTimes, cancellationToken);
    }

    private async Task EnsureTripExistsAsync(Guid agencyId, Guid tripId, CancellationToken cancellationToken)
    {
        if (await tripRepository.GetAsync(agencyId, tripId, cancellationToken) is null)
        {
            throw TripNotFound(tripId);
        }
    }

    private static void Normalize(ITripModel trip)
    {
        trip.Code = trip.Code?.Trim() ?? string.Empty;
        trip.Headsign = NullIfBlank(trip.Headsign);
        trip.ShortName = NullIfBlank(trip.ShortName);
        trip.BlockId = NullIfBlank(trip.BlockId);
    }

    private async Task ValidateAsync(ITripModel trip, CancellationToken cancellationToken)
    {
        var errors = new ValidationErrors();

        if (trip.Code.Length == 0)
        {
            errors.Add(nameof(trip.Code), "Kod je obavezan.");
        }

        if (trip.DirectionId is { } direction && !Enum.IsDefined(direction))
        {
            errors.Add(nameof(trip.DirectionId), "Smjer mora biti 0 ili 1.");
        }

        if (!Enum.IsDefined(trip.WheelchairAccessible))
        {
            errors.Add(nameof(trip.WheelchairAccessible), "Nepoznata vrijednost pristupačnosti za invalidska kolica.");
        }

        if (!Enum.IsDefined(trip.BikesAllowed))
        {
            errors.Add(nameof(trip.BikesAllowed), "Nepoznata vrijednost za prijevoz bicikala.");
        }

        // Shema ne povezuje agency_id vožnje s agencijom linije i kalendara, pa to provjeravamo ovdje.
        if (await routeRepository.GetAsync(trip.AgencyId, trip.RouteId, cancellationToken) is null)
        {
            errors.Add(nameof(trip.RouteId), "Linija ne postoji.");
        }

        if (await calendarRepository.GetAsync(trip.AgencyId, trip.ServiceCalendarId, cancellationToken) is null)
        {
            errors.Add(nameof(trip.ServiceCalendarId), "Kalendar ne postoji.");
        }

        errors.ThrowIfAny();
    }

    private async Task ValidateStopTimesAsync(Guid agencyId, IReadOnlyList<IStopTimeModel> stopTimes, CancellationToken cancellationToken)
    {
        var errors = new ValidationErrors();

        if (stopTimes.Count < MinStopTimes)
        {
            errors.Add("StopTimes", $"Vožnja mora imati barem {MinStopTimes} stanice.");
            errors.ThrowIfAny();
        }

        var stopIds = stopTimes.Select(st => st.StopId).Distinct().ToList();
        var stops = (await stopRepository.GetManyAsync(agencyId, stopIds, cancellationToken))
            .ToDictionary(s => s.Id);

        for (var i = 0; i < stopTimes.Count; i++)
        {
            var stopTime = stopTimes[i];
            var prefix = $"StopTimes[{i}]";

            if (!stops.TryGetValue(stopTime.StopId, out var stop))
            {
                errors.Add($"{prefix}.StopId", "Stanica ne postoji.");
            }
            else if (stop.LocationType == LocationType.Station)
            {
                // GTFS: vožnja staje na stajalištu ili peronu, ne na roditeljskoj stanici.
                errors.Add($"{prefix}.StopId", $"'{stop.Name}' je kolodvor; odaberi njegov peron ili stajalište.");
            }

            if (stopTime.ArrivalTime is < 0 or > GtfsTime.MaxSeconds)
            {
                errors.Add($"{prefix}.ArrivalTime", "Vrijeme dolaska mora biti između 00:00:00 i 48:00:00.");
            }

            if (stopTime.DepartureTime is < 0 or > GtfsTime.MaxSeconds)
            {
                errors.Add($"{prefix}.DepartureTime", "Vrijeme polaska mora biti između 00:00:00 i 48:00:00.");
            }

            if (stopTime.DepartureTime < stopTime.ArrivalTime)
            {
                errors.Add($"{prefix}.DepartureTime", "Polazak ne smije biti prije dolaska.");
            }

            if (i > 0 && stopTime.ArrivalTime < stopTimes[i - 1].DepartureTime)
            {
                errors.Add(
                    $"{prefix}.ArrivalTime",
                    $"Dolazak ({GtfsTime.Format(stopTime.ArrivalTime)}) je prije polaska s prethodne stanice ({GtfsTime.Format(stopTimes[i - 1].DepartureTime)}).");
            }

            if (!Enum.IsDefined(stopTime.PickupType))
            {
                errors.Add($"{prefix}.PickupType", "Nepoznata vrsta ukrcavanja.");
            }

            if (!Enum.IsDefined(stopTime.DropOffType))
            {
                errors.Add($"{prefix}.DropOffType", "Nepoznata vrsta iskrcavanja.");
            }
        }

        errors.ThrowIfAny();
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static NotFoundException TripNotFound(Guid id) => new($"Vožnja {id} ne postoji.");
}
