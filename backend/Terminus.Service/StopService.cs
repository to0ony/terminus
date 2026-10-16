using Terminus.Common;
using Terminus.Common.Exceptions;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Model.Common.Filters;
using Terminus.Repository.Common;
using Terminus.Service.Common;
using static Terminus.Service.Validators;

namespace Terminus.Service;

public class StopService(IStopRepository repository) : IStopService
{
    public const int MaxAreaStops = 2000;

    public async Task<IStopModel> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        await repository.GetAsync(id, cancellationToken) ?? throw StopNotFound(id);

    public Task<PagedResult<IStopModel>> FindAsync(StopFilter filter, CancellationToken cancellationToken = default)
    {
        var errors = new ValidationErrors();
        errors.AddPagingErrors(filter.Page, filter.PageSize);
        errors.ThrowIfAny();

        return repository.FindAsync(filter, cancellationToken);
    }

    public async Task<StopsInArea> FindInAreaAsync(GeoBounds bounds, bool? isActive, CancellationToken cancellationToken = default)
    {
        var errors = new ValidationErrors();

        if (bounds.MinLatitude is < -90 or > 90 || bounds.MaxLatitude is < -90 or > 90)
        {
            errors.Add(nameof(bounds.MinLatitude), "Geografska širina mora biti između -90 i 90.");
        }
        else if (bounds.MinLatitude > bounds.MaxLatitude)
        {
            errors.Add(nameof(bounds.MinLatitude), "Južni rub ne smije biti sjevernije od sjevernog.");
        }

        if (bounds.MinLongitude is < -180 or > 180 || bounds.MaxLongitude is < -180 or > 180)
        {
            errors.Add(nameof(bounds.MinLongitude), "Geografska dužina mora biti između -180 i 180.");
        }
        else if (bounds.MinLongitude > bounds.MaxLongitude)
        {
            errors.Add(nameof(bounds.MinLongitude), "Zapadni rub ne smije biti istočnije od istočnog.");
        }

        errors.ThrowIfAny();

        // Jedna stanica više govori je li rezultat skraćen.
        var stops = await repository.FindInAreaAsync(bounds, isActive, MaxAreaStops + 1, cancellationToken);

        return stops.Count > MaxAreaStops
            ? new StopsInArea(stops.Take(MaxAreaStops).ToList(), Truncated: true)
            : new StopsInArea(stops, Truncated: false);
    }

    public async Task<IStopModel> CreateAsync(IStopModel stop, CancellationToken cancellationToken = default)
    {
        stop.Id = Guid.Empty;
        Normalize(stop);

        await ValidateAsync(stop, existing: null, cancellationToken);

        return await repository.InsertAsync(stop, cancellationToken);
    }

    public async Task<IStopModel> UpdateAsync(Guid id, IStopModel stop, CancellationToken cancellationToken = default)
    {
        var existing = await repository.GetAsync(id, cancellationToken) ?? throw StopNotFound(id);

        stop.Id = id;
        Normalize(stop);

        await ValidateAsync(stop, existing, cancellationToken);

        return await repository.UpdateAsync(stop, cancellationToken) ?? throw StopNotFound(id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await repository.DeleteAsync(id, cancellationToken))
        {
            throw StopNotFound(id);
        }
    }

    /// <summary>
    /// Koja vrsta roditelja je potrebna (GTFS parent_station): null znači da roditelj
    /// nije dopušten, a <c>Required</c> da je obavezan.
    /// </summary>
    internal static (LocationType? ParentType, bool Required) ParentRule(LocationType type) => type switch
    {
        LocationType.Station => (null, false),
        LocationType.StopOrPlatform => (LocationType.Station, false),
        LocationType.Entrance or LocationType.GenericNode => (LocationType.Station, true),
        LocationType.BoardingArea => (LocationType.StopOrPlatform, true),
        _ => (null, false)
    };

    /// <summary>GTFS: koordinate su obavezne osim za čvorove i mjesta ukrcavanja.</summary>
    internal static bool RequiresCoordinates(LocationType type) =>
        type is not (LocationType.GenericNode or LocationType.BoardingArea);

    private static void Normalize(IStopModel stop)
    {
        stop.Code = stop.Code?.Trim() ?? string.Empty;
        stop.PublicCode = NullIfBlank(stop.PublicCode);
        stop.Name = stop.Name?.Trim() ?? string.Empty;
        stop.TtsName = NullIfBlank(stop.TtsName);
        stop.Description = NullIfBlank(stop.Description);
        stop.ZoneId = NullIfBlank(stop.ZoneId);
        stop.Url = NullIfBlank(stop.Url);
        stop.Timezone = NullIfBlank(stop.Timezone);
        stop.PlatformCode = NullIfBlank(stop.PlatformCode);
    }

    private async Task ValidateAsync(IStopModel stop, IStopModel? existing, CancellationToken cancellationToken)
    {
        var errors = new ValidationErrors();

        if (stop.Code.Length == 0)
        {
            errors.Add(nameof(stop.Code), "Kod je obavezan.");
        }

        if (stop.Name.Length == 0)
        {
            errors.Add(nameof(stop.Name), "Naziv je obavezan.");
        }

        if (!Enum.IsDefined(stop.LocationType))
        {
            errors.Add(nameof(stop.LocationType), "Nepoznata vrsta lokacije.");
            errors.ThrowIfAny();
        }

        ValidateCoordinates(stop, errors);

        if (!Enum.IsDefined(stop.WheelchairBoarding))
        {
            errors.Add(nameof(stop.WheelchairBoarding), "Nepoznata vrijednost pristupačnosti za invalidska kolica.");
        }

        if (stop.Url is not null && !IsHttpUrl(stop.Url))
        {
            errors.Add(nameof(stop.Url), "URL mora biti potpuna http ili https adresa.");
        }

        if (stop.Timezone is not null && !IsIanaTimezone(stop.Timezone))
        {
            errors.Add(nameof(stop.Timezone), $"Nepoznata vremenska zona '{stop.Timezone}'. Koristi IANA naziv, npr. Europe/Zagreb.");
        }

        await ValidateParentAsync(stop, errors, cancellationToken);

        if (existing is not null && existing.LocationType != stop.LocationType)
        {
            if (await repository.HasChildStopsAsync(stop.Id, cancellationToken))
            {
                errors.Add(nameof(stop.LocationType), "Stanica ima podređene zapise (perone, ulaze ili mjesta ukrcavanja), pa joj se vrsta ne može promijeniti.");
            }
            else if (existing.LocationType == LocationType.StopOrPlatform
                     && await repository.IsUsedByTripsAsync(stop.Id, cancellationToken))
            {
                errors.Add(nameof(stop.LocationType), "Na stanici staju vožnje, pa mora ostati stajalište ili peron.");
            }
        }

        errors.ThrowIfAny();
    }

    private static void ValidateCoordinates(IStopModel stop, ValidationErrors errors)
    {
        if (stop.Latitude is null != stop.Longitude is null)
        {
            errors.Add(nameof(stop.Latitude), "Geografska širina i dužina zadaju se zajedno.");
            return;
        }

        if (stop.Latitude is null)
        {
            if (RequiresCoordinates(stop.LocationType))
            {
                errors.Add(nameof(stop.Latitude), "Koordinate su obavezne.");
            }

            return;
        }

        if (!double.IsFinite(stop.Latitude.Value) || stop.Latitude is < -90 or > 90)
        {
            errors.Add(nameof(stop.Latitude), "Geografska širina mora biti između -90 i 90.");
        }

        if (!double.IsFinite(stop.Longitude!.Value) || stop.Longitude is < -180 or > 180)
        {
            errors.Add(nameof(stop.Longitude), "Geografska dužina mora biti između -180 i 180.");
        }
    }

    private async Task ValidateParentAsync(IStopModel stop, ValidationErrors errors, CancellationToken cancellationToken)
    {
        var (parentType, required) = ParentRule(stop.LocationType);

        if (stop.ParentStationId is not Guid parentStationId)
        {
            if (required)
            {
                errors.Add(nameof(stop.ParentStationId), parentType == LocationType.Station
                    ? "Ulaz i čvor moraju pripadati stanici (kolodvoru)."
                    : "Mjesto ukrcavanja mora pripadati peronu.");
            }

            return;
        }

        if (parentType is null)
        {
            errors.Add(nameof(stop.ParentStationId), "Kolodvor ne može imati nadređenu stanicu.");
            return;
        }

        if (parentStationId == stop.Id)
        {
            errors.Add(nameof(stop.ParentStationId), "Stanica ne može biti sama sebi nadređena.");
            return;
        }

        var parent = await repository.GetAsync(parentStationId, cancellationToken);
        if (parent is null)
        {
            errors.Add(nameof(stop.ParentStationId), "Nadređena stanica ne postoji.");
        }
        else if (parent.LocationType != parentType)
        {
            errors.Add(nameof(stop.ParentStationId), parentType == LocationType.Station
                ? "Nadređena stanica mora biti kolodvor."
                : "Nadređena stanica mjesta ukrcavanja mora biti peron.");
        }
    }

    private static NotFoundException StopNotFound(Guid id) => new($"Stanica {id} ne postoji.");
}
