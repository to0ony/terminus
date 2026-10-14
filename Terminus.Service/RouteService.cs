using System.Text.RegularExpressions;
using Terminus.Common;
using Terminus.Common.Exceptions;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;
using Terminus.Repository.Common;
using Terminus.Service.Common;

namespace Terminus.Service;

public partial class RouteService(IRouteRepository repository) : IRouteService
{
    public async Task<IRouteModel> GetAsync(Guid agencyId, Guid id, CancellationToken cancellationToken = default) =>
        await repository.GetAsync(agencyId, id, cancellationToken) ?? throw RouteNotFound(id);

    public Task<PagedResult<IRouteModel>> FindAsync(Guid agencyId, RouteFilter filter, CancellationToken cancellationToken = default)
    {
        var errors = new ValidationErrors();
        errors.AddPagingErrors(filter.Page, filter.PageSize);
        errors.ThrowIfAny();

        return repository.FindAsync(agencyId, filter, cancellationToken);
    }

    public async Task<IRouteModel> CreateAsync(Guid agencyId, IRouteModel route, CancellationToken cancellationToken = default)
    {
        route.Id = Guid.Empty;
        route.AgencyId = agencyId;
        Normalize(route);
        Validate(route);

        return await repository.InsertAsync(route, cancellationToken);
    }

    public async Task<IRouteModel> UpdateAsync(Guid agencyId, Guid id, IRouteModel route, CancellationToken cancellationToken = default)
    {
        route.Id = id;
        route.AgencyId = agencyId;
        Normalize(route);
        Validate(route);

        return await repository.UpdateAsync(route, cancellationToken) ?? throw RouteNotFound(id);
    }

    public async Task DeleteAsync(Guid agencyId, Guid id, CancellationToken cancellationToken = default)
    {
        if (!await repository.DeleteAsync(agencyId, id, cancellationToken))
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
        route.Color = NormalizeColor(route.Color);
        route.TextColor = NormalizeColor(route.TextColor);
    }

    private static void Validate(IRouteModel route)
    {
        var errors = new ValidationErrors();

        if (route.Code.Length == 0)
        {
            errors.Add(nameof(route.Code), "Kod je obavezan.");
        }

        if (route.ShortName is null && route.LongName is null)
        {
            errors.Add(nameof(route.ShortName), "Potrebna je kratka oznaka ili puni naziv linije.");
        }

        if (!Enum.IsDefined(route.RouteType))
        {
            errors.Add(nameof(route.RouteType), "Nepoznata vrsta prijevoza.");
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
