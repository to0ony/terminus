using Terminus.Common.Exceptions;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Repository.Common;
using Terminus.Service.Common;

namespace Terminus.Service;

public class RouteVariantService(
    IRouteVariantRepository variantRepository,
    IRouteRepository routeRepository,
    IStopRepository stopRepository,
    IShapeRepository shapeRepository,
    ITimetableRepository timetableRepository) : IRouteVariantService
{
    public const int MaxNameLength = 100;

    /// <summary>Najdulje vrijeme vožnje dionice; dulje je gotovo sigurno greška.</summary>
    public const int MaxTravelTime = 4 * 3600;

    /// <summary>Najdulje stajanje na stanici.</summary>
    public const int MaxDwellTime = 3600;

    public async Task<RouteVariants> ListAsync(Guid routeId, CancellationToken cancellationToken = default)
    {
        await EnsureRouteExistsAsync(routeId, cancellationToken);
        var variants = await variantRepository.ListByRouteAsync(routeId, cancellationToken);
        return await WithStopsAsync(variants, cancellationToken);
    }

    public async Task<RouteVariantDetail> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var variant = await variantRepository.GetAsync(id, cancellationToken) ?? throw VariantNotFound(id);
        return await DetailAsync(variant, cancellationToken);
    }

    public async Task<RouteVariantDetail> CreateAsync(Guid routeId, IRouteVariantModel variant, CancellationToken cancellationToken = default)
    {
        await EnsureRouteExistsAsync(routeId, cancellationToken);
        variant.Id = Guid.Empty;
        variant.RouteId = routeId;
        variant.Source = VariantSource.Manual;

        // Nova varijanta ide iza postojećih istog smjera.
        var existing = await variantRepository.ListByRouteAsync(routeId, cancellationToken);
        variant.SortOrder = existing.Count(v => v.DirectionId == variant.DirectionId);

        Normalize(variant);
        await ValidateAsync(variant, cancellationToken);

        var created = await variantRepository.InsertAsync(variant, cancellationToken);
        return await DetailAsync(created, cancellationToken);
    }

    public async Task<RouteVariantDetail> UpdateAsync(Guid id, IRouteVariantModel variant, CancellationToken cancellationToken = default)
    {
        var current = await variantRepository.GetAsync(id, cancellationToken) ?? throw VariantNotFound(id);
        variant.Id = id;
        variant.RouteId = current.RouteId;
        // Uređena varijanta je ručna, pa je ponovni GTFS uvoz ne zamjenjuje.
        variant.Source = VariantSource.Manual;
        variant.SortOrder = current.SortOrder;

        Normalize(variant);
        await ValidateAsync(variant, cancellationToken);

        var stopsChanged = !current.Stops.Select(s => s.StopId).SequenceEqual(variant.Stops.Select(s => s.StopId));
        if (stopsChanged)
        {
            var trips = (await variantRepository.CountTripsAsync([id], cancellationToken)).GetValueOrDefault(id);
            if (trips > 0)
            {
                throw new ConflictException(
                    $"Varijantu koristi {trips} vožnji, pa joj se stanice ne mogu promijeniti. Napravi novu varijantu ili prvo obriši vožnje.");
            }
        }

        var updated = await variantRepository.UpdateAsync(variant, cancellationToken) ?? throw VariantNotFound(id);
        return await DetailAsync(updated, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var trips = (await variantRepository.CountTripsAsync([id], cancellationToken)).GetValueOrDefault(id);
        if (trips > 0)
        {
            throw new ConflictException($"Varijantu koristi {trips} vožnji, pa se ne može obrisati.");
        }
        if (!await variantRepository.DeleteAsync(id, cancellationToken))
        {
            throw VariantNotFound(id);
        }
    }

    public async Task<RouteVariants> DeriveAsync(Guid routeId, CancellationToken cancellationToken = default)
    {
        await EnsureRouteExistsAsync(routeId, cancellationToken);
        if ((await variantRepository.ListByRouteAsync(routeId, cancellationToken)).Count > 0)
        {
            throw new ConflictException("Linija već ima varijante.");
        }

        var trips = await timetableRepository.GetRouteTripsAsync(routeId, null, null, cancellationToken);
        var stopIds = trips.SelectMany(t => t.StopTimes).Select(st => st.StopId).Distinct().ToList();
        var stops = (await stopRepository.GetManyAsync(stopIds, cancellationToken)).ToDictionary(s => s.Id);

        var derived = RouteVariantBuilder.Derive(routeId, trips, id => stops.TryGetValue(id, out var stop) ? stop.Name : "?");
        var assignments = derived
            .SelectMany(d => d.TripIds.Select(tripId => (tripId, d.Variant.Id)))
            .ToDictionary(x => x.tripId, x => x.Item2);

        await variantRepository.InsertManyAsync(derived.Select(d => d.Variant).ToList(), assignments, cancellationToken);
        return await ListAsync(routeId, cancellationToken);
    }

    private async Task EnsureRouteExistsAsync(Guid routeId, CancellationToken cancellationToken)
    {
        if (await routeRepository.GetAsync(routeId, cancellationToken) is null)
        {
            throw new NotFoundException($"Linija {routeId} ne postoji.");
        }
    }

    private async Task<RouteVariants> WithStopsAsync(IReadOnlyList<IRouteVariantModel> variants, CancellationToken cancellationToken)
    {
        var stopIds = variants.SelectMany(v => v.Stops).Select(s => s.StopId).Distinct().ToList();
        var stops = (await stopRepository.GetManyAsync(stopIds, cancellationToken)).ToDictionary(s => s.Id);
        var counts = await variantRepository.CountTripsAsync(variants.Select(v => v.Id).ToList(), cancellationToken);
        return new RouteVariants(variants, stops, counts);
    }

    private async Task<RouteVariantDetail> DetailAsync(IRouteVariantModel variant, CancellationToken cancellationToken)
    {
        var list = await WithStopsAsync([variant], cancellationToken);
        return new RouteVariantDetail(variant, list.Stops, list.TripCounts.GetValueOrDefault(variant.Id));
    }

    private static void Normalize(IRouteVariantModel variant)
    {
        variant.Name = variant.Name?.Trim() ?? string.Empty;
        variant.Headsign = string.IsNullOrWhiteSpace(variant.Headsign) ? null : variant.Headsign.Trim();
        // Prva stanica nema vremena vožnje od prethodne.
        variant.Stops = variant.Stops
            .Select((s, i) => i == 0 ? s with { TravelTime = 0 } : s)
            .ToList();
    }

    private async Task ValidateAsync(IRouteVariantModel variant, CancellationToken cancellationToken)
    {
        var errors = new ValidationErrors();

        if (variant.Name.Length == 0)
        {
            errors.Add("Name", "Naziv je obavezan.");
        }
        else if (variant.Name.Length > MaxNameLength)
        {
            errors.Add("Name", $"Naziv smije imati najviše {MaxNameLength} znakova.");
        }

        if (variant.DirectionId is { } direction && !Enum.IsDefined(direction))
        {
            errors.Add("DirectionId", "Smjer mora biti 0 ili 1.");
        }

        if (variant.ShapeId is { } shapeId && !await shapeRepository.ExistsAsync(shapeId, cancellationToken))
        {
            errors.Add("ShapeId", "Trasa ne postoji.");
        }

        if (variant.Stops.Count < StopTimeRules.MinStopTimes)
        {
            errors.Add("Stops", $"Varijanta mora imati barem {StopTimeRules.MinStopTimes} stanice.");
        }
        else
        {
            var stops = (await stopRepository.GetManyAsync(variant.Stops.Select(s => s.StopId).Distinct().ToList(), cancellationToken))
                .ToDictionary(s => s.Id);

            for (var i = 0; i < variant.Stops.Count; i++)
            {
                var stop = variant.Stops[i];
                var prefix = $"Stops[{i}]";

                if (!stops.TryGetValue(stop.StopId, out var model))
                {
                    errors.Add($"{prefix}.StopId", "Stanica ne postoji.");
                }
                else if (model.LocationType != LocationType.StopOrPlatform)
                {
                    // GTFS: vožnja staje na stajalištu ili peronu, ne na kolodvoru, ulazu ni čvoru.
                    errors.Add($"{prefix}.StopId", $"'{model.Name}' nije stajalište ni peron; za kolodvor odaberi peron.");
                }

                if (i > 0 && variant.Stops[i - 1].StopId == stop.StopId)
                {
                    errors.Add($"{prefix}.StopId", "Ista stanica ne može biti dvaput zaredom.");
                }
                if (stop.TravelTime is < 0 or > MaxTravelTime)
                {
                    errors.Add($"{prefix}.TravelTime", "Vrijeme vožnje mora biti između 0 i 4 sata.");
                }
                if (stop.DwellTime is < 0 or > MaxDwellTime)
                {
                    errors.Add($"{prefix}.DwellTime", "Stajanje mora biti između 0 i 60 minuta.");
                }
            }
        }

        errors.ThrowIfAny();
    }

    private static NotFoundException VariantNotFound(Guid id) => new($"Varijanta {id} ne postoji.");
}
