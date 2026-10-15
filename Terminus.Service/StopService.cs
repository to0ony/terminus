using Terminus.Common;
using Terminus.Common.Exceptions;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Model.Common.Filters;
using Terminus.Repository.Common;
using Terminus.Service.Common;

namespace Terminus.Service;

public class StopService(IStopRepository repository) : IStopService
{
    public async Task<IStopModel> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        await repository.GetAsync(id, cancellationToken) ?? throw StopNotFound(id);

    public Task<PagedResult<IStopModel>> FindAsync(StopFilter filter, CancellationToken cancellationToken = default)
    {
        var errors = new ValidationErrors();
        errors.AddPagingErrors(filter.Page, filter.PageSize);
        errors.ThrowIfAny();

        return repository.FindAsync(filter, cancellationToken);
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

    private static void Normalize(IStopModel stop)
    {
        stop.Code = stop.Code?.Trim() ?? string.Empty;
        stop.Name = stop.Name?.Trim() ?? string.Empty;
        stop.Description = NullIfBlank(stop.Description);
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

        if (!double.IsFinite(stop.Latitude) || stop.Latitude is < -90 or > 90)
        {
            errors.Add(nameof(stop.Latitude), "Geografska širina mora biti između -90 i 90.");
        }

        if (!double.IsFinite(stop.Longitude) || stop.Longitude is < -180 or > 180)
        {
            errors.Add(nameof(stop.Longitude), "Geografska dužina mora biti između -180 i 180.");
        }

        if (!Enum.IsDefined(stop.LocationType))
        {
            errors.Add(nameof(stop.LocationType), "Nepoznata vrsta lokacije.");
        }

        if (!Enum.IsDefined(stop.WheelchairBoarding))
        {
            errors.Add(nameof(stop.WheelchairBoarding), "Nepoznata vrijednost pristupačnosti za invalidska kolica.");
        }

        if (stop.ParentStationId is Guid parentStationId)
        {
            if (stop.LocationType == LocationType.Station)
            {
                errors.Add(nameof(stop.ParentStationId), "Kolodvor ne može imati nadređenu stanicu.");
            }
            else if (parentStationId == stop.Id)
            {
                errors.Add(nameof(stop.ParentStationId), "Stanica ne može biti sama sebi nadređena.");
            }
            else
            {
                var parent = await repository.GetAsync(parentStationId, cancellationToken);
                if (parent is null)
                {
                    errors.Add(nameof(stop.ParentStationId), "Nadređena stanica ne postoji.");
                }
                else if (parent.LocationType != LocationType.Station)
                {
                    errors.Add(nameof(stop.ParentStationId), "Nadređena stanica mora biti kolodvor.");
                }
            }
        }

        if (existing?.LocationType == LocationType.Station
            && stop.LocationType != LocationType.Station
            && await repository.HasChildStopsAsync(stop.Id, cancellationToken))
        {
            errors.Add(nameof(stop.LocationType), "Kolodvor ima perone pa se ne može pretvoriti u stajalište.");
        }

        errors.ThrowIfAny();
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static NotFoundException StopNotFound(Guid id) => new($"Stanica {id} ne postoji.");
}
