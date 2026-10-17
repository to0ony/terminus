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
    IStopRepository stopRepository,
    IShapeRepository shapeRepository,
    IRouteVariantRepository variantRepository) : ITripService
{
    public async Task<ITripModel> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        await tripRepository.GetAsync(id, cancellationToken) ?? throw TripNotFound(id);

    public Task<PagedResult<ITripModel>> FindAsync(TripFilter filter, CancellationToken cancellationToken = default)
    {
        var errors = new ValidationErrors();
        errors.AddPagingErrors(filter.Page, filter.PageSize);
        errors.ThrowIfAny();

        return tripRepository.FindAsync(filter, cancellationToken);
    }

    public async Task<ITripModel> CreateAsync(ITripModel trip, CancellationToken cancellationToken = default)
    {
        trip.Id = Guid.Empty;
        Normalize(trip);
        await ValidateAsync(trip, cancellationToken);

        return await tripRepository.InsertAsync(trip, cancellationToken);
    }

    public async Task<ITripModel> UpdateAsync(Guid id, ITripModel trip, CancellationToken cancellationToken = default)
    {
        trip.Id = id;
        Normalize(trip);
        await ValidateAsync(trip, cancellationToken);

        return await tripRepository.UpdateAsync(trip, cancellationToken) ?? throw TripNotFound(id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await tripRepository.DeleteAsync(id, cancellationToken))
        {
            throw TripNotFound(id);
        }
    }

    public async Task<IReadOnlyList<IStopTimeModel>> GetStopTimesAsync(Guid tripId, CancellationToken cancellationToken = default)
    {
        await EnsureTripExistsAsync(tripId, cancellationToken);
        return await stopTimeRepository.GetByTripAsync(tripId, cancellationToken);
    }

    public async Task<IReadOnlyList<IStopTimeModel>> ReplaceStopTimesAsync(
        Guid tripId,
        IReadOnlyList<IStopTimeModel> stopTimes,
        CancellationToken cancellationToken = default)
    {
        await EnsureTripExistsAsync(tripId, cancellationToken);

        for (var i = 0; i < stopTimes.Count; i++)
        {
            stopTimes[i].TripId = tripId;
            stopTimes[i].StopSequence = i + 1;
            stopTimes[i].StopHeadsign = NullIfBlank(stopTimes[i].StopHeadsign);
        }

        await ValidateStopTimesAsync(stopTimes, cancellationToken);

        return await stopTimeRepository.ReplaceForTripAsync(tripId, stopTimes, cancellationToken);
    }

    private async Task EnsureTripExistsAsync(Guid tripId, CancellationToken cancellationToken)
    {
        if (await tripRepository.GetAsync(tripId, cancellationToken) is null)
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

        if (await routeRepository.GetAsync(trip.RouteId, cancellationToken) is null)
        {
            errors.Add(nameof(trip.RouteId), "Linija ne postoji.");
        }

        if (await calendarRepository.GetAsync(trip.ServiceCalendarId, cancellationToken) is null)
        {
            errors.Add(nameof(trip.ServiceCalendarId), "Kalendar ne postoji.");
        }

        if (trip.ShapeId is Guid shapeId && !await shapeRepository.ExistsAsync(shapeId, cancellationToken))
        {
            errors.Add(nameof(trip.ShapeId), "Trasa ne postoji.");
        }

        if (trip.VariantId is Guid variantId
            && (await variantRepository.GetAsync(variantId, cancellationToken))?.RouteId != trip.RouteId)
        {
            errors.Add(nameof(trip.VariantId), "Varijanta ne postoji ili ne pripada liniji vožnje.");
        }

        errors.ThrowIfAny();
    }

    private async Task ValidateStopTimesAsync(IReadOnlyList<IStopTimeModel> stopTimes, CancellationToken cancellationToken)
    {
        var errors = new ValidationErrors();

        if (stopTimes.Count < StopTimeRules.MinStopTimes)
        {
            errors.Add("StopTimes", $"Vožnja mora imati barem {StopTimeRules.MinStopTimes} stanice.");
            errors.ThrowIfAny();
        }

        var stopIds = stopTimes.Select(st => st.StopId).Distinct().ToList();
        var stops = (await stopRepository.GetManyAsync(stopIds, cancellationToken))
            .ToDictionary(s => s.Id);

        StopTimeRules.Validate(stopTimes, stops, errors, "StopTimes");

        errors.ThrowIfAny();
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static NotFoundException TripNotFound(Guid id) => new($"Vožnja {id} ne postoji.");
}
