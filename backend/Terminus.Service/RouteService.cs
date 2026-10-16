using System.Text.RegularExpressions;
using Terminus.Common;
using Terminus.Common.Exceptions;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;
using Terminus.Repository.Common;
using Terminus.Service.Common;

namespace Terminus.Service;

public partial class RouteService(IRouteRepository repository, IAgencyRepository agencyRepository) : IRouteService
{
    public async Task<IRouteModel> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        await repository.GetAsync(id, cancellationToken) ?? throw RouteNotFound(id);

    public Task<PagedResult<IRouteModel>> FindAsync(RouteFilter filter, CancellationToken cancellationToken = default)
    {
        var errors = new ValidationErrors();
        errors.AddPagingErrors(filter.Page, filter.PageSize);
        errors.ThrowIfAny();

        return repository.FindAsync(filter, cancellationToken);
    }

    public async Task<IRouteModel> CreateAsync(IRouteModel route, CancellationToken cancellationToken = default)
    {
        route.Id = Guid.Empty;
        Normalize(route);
        await ValidateAsync(route, cancellationToken);

        return await repository.InsertAsync(route, cancellationToken);
    }

    public async Task<IRouteModel> UpdateAsync(Guid id, IRouteModel route, CancellationToken cancellationToken = default)
    {
        route.Id = id;
        Normalize(route);
        await ValidateAsync(route, cancellationToken);

        return await repository.UpdateAsync(route, cancellationToken) ?? throw RouteNotFound(id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await repository.DeleteAsync(id, cancellationToken))
        {
            throw RouteNotFound(id);
        }
    }

    private static void Normalize(IRouteModel route)
    {
        route.Code = route.Code?.Trim() ?? string.Empty;
        route.ShortName = NullIfBlank(route.ShortName);
        route.LongName = NullIfBlank(route.LongName);
        route.Description = NullIfBlank(route.Description);
        route.Url = NullIfBlank(route.Url);
        route.NetworkId = NullIfBlank(route.NetworkId);
        route.Color = NormalizeColor(route.Color);
        route.TextColor = NormalizeColor(route.TextColor);
    }

    private async Task ValidateAsync(IRouteModel route, CancellationToken cancellationToken)
    {
        var errors = new ValidationErrors();

        // GTFS routes.agency_id: linija mora imati operatera.
        if (await agencyRepository.GetAsync(route.AgencyId, cancellationToken) is null)
        {
            errors.Add(nameof(route.AgencyId), "Prijevoznik ne postoji.");
        }

        if (route.Code.Length == 0)
        {
            errors.Add(nameof(route.Code), "Kod je obavezan.");
        }

        if (route.ShortName is null && route.LongName is null)
        {
            errors.Add(nameof(route.ShortName), "Potrebna je kratka oznaka ili puni naziv linije.");
        }

        if (!RouteTypes.IsValid(route.RouteType))
        {
            errors.Add(nameof(route.RouteType),
                $"Nepoznata vrsta prijevoza. Dopuštene su osnovne GTFS vrijednosti i proširene od {RouteTypes.ExtendedMin} do {RouteTypes.ExtendedMax}.");
        }

        if (route.Url is not null && !Validators.IsHttpUrl(route.Url))
        {
            errors.Add(nameof(route.Url), "URL mora biti potpuna http ili https adresa.");
        }

        if (route.ContinuousPickup is { } pickup && !Enum.IsDefined(pickup))
        {
            errors.Add(nameof(route.ContinuousPickup), "Nepoznata vrijednost za ukrcavanje duž trase.");
        }

        if (route.ContinuousDropOff is { } dropOff && !Enum.IsDefined(dropOff))
        {
            errors.Add(nameof(route.ContinuousDropOff), "Nepoznata vrijednost za iskrcavanje duž trase.");
        }

        if (route.Color is not null && !HexColorRegex().IsMatch(route.Color))
        {
            errors.Add(nameof(route.Color), "Boja mora biti hex vrijednost od 6 znakova, npr. FF0000.");
        }

        if (route.TextColor is not null && !HexColorRegex().IsMatch(route.TextColor))
        {
            errors.Add(nameof(route.TextColor), "Boja teksta mora biti hex vrijednost od 6 znakova, npr. FFFFFF.");
        }

        if (route.SortOrder < 0)
        {
            errors.Add(nameof(route.SortOrder), "Redoslijed ne smije biti negativan.");
        }

        errors.ThrowIfAny();
    }

    /// <summary>"#ff0000" postaje "FF0000" (GTFS traži hex bez #).</summary>
    private static string? NormalizeColor(string? value) =>
        NullIfBlank(value)?.TrimStart('#').ToUpperInvariant();

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static NotFoundException RouteNotFound(Guid id) => new($"Linija {id} ne postoji.");

    [GeneratedRegex("^[0-9A-F]{6}$")]
    private static partial Regex HexColorRegex();
}
