using System.Globalization;
using System.Net.Mail;
using Terminus.Common;
using Terminus.Common.Exceptions;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;
using Terminus.Repository.Common;
using Terminus.Service.Common;

namespace Terminus.Service;

public class AgencyService(IAgencyRepository repository) : IAgencyService
{
    private const string DefaultTimezone = "Europe/Zagreb";
    private const string DefaultLang = "hr";

    public async Task<IAgencyModel> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        await repository.GetAsync(id, cancellationToken) ?? throw AgencyNotFound(id);

    public Task<PagedResult<IAgencyModel>> FindAsync(AgencyFilter filter, CancellationToken cancellationToken = default)
    {
        var errors = new ValidationErrors();
        errors.AddPagingErrors(filter.Page, filter.PageSize);
        errors.ThrowIfAny();

        return repository.FindAsync(filter, cancellationToken);
    }

    public async Task<IAgencyModel> CreateAsync(IAgencyModel agency, CancellationToken cancellationToken = default)
    {
        agency.Id = Guid.Empty;
        Normalize(agency);
        Validate(agency);

        return await repository.InsertAsync(agency, cancellationToken);
    }

    public async Task<IAgencyModel> UpdateAsync(Guid id, IAgencyModel agency, CancellationToken cancellationToken = default)
    {
        agency.Id = id;
        Normalize(agency);
        Validate(agency);

        return await repository.UpdateAsync(agency, cancellationToken) ?? throw AgencyNotFound(id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await repository.DeleteAsync(id, cancellationToken))
        {
            throw AgencyNotFound(id);
        }
    }

    private static void Normalize(IAgencyModel agency)
    {
        agency.Name = agency.Name?.Trim() ?? string.Empty;
        agency.Url = agency.Url?.Trim() ?? string.Empty;
        agency.Timezone = NullIfBlank(agency.Timezone) ?? DefaultTimezone;
        agency.Lang = NullIfBlank(agency.Lang) ?? DefaultLang;
        agency.Phone = NullIfBlank(agency.Phone);
        agency.Email = NullIfBlank(agency.Email);
    }

    private static void Validate(IAgencyModel agency)
    {
        var errors = new ValidationErrors();

        if (agency.Name.Length == 0)
        {
            errors.Add(nameof(agency.Name), "Naziv je obavezan.");
        }

        // GTFS agency.txt traži agency_url.
        if (agency.Url.Length == 0)
        {
            errors.Add(nameof(agency.Url), "URL je obavezan.");
        }
        else if (!(Uri.TryCreate(agency.Url, UriKind.Absolute, out var uri)
                   && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
        {
            errors.Add(nameof(agency.Url), "URL mora biti potpuna http ili https adresa.");
        }

        // GTFS traži IANA naziv. TryFindSystemTimeZoneById bi na Windowsu prihvatio i
        // Windows nazive ("Central European Standard Time"), pa provjeravamo i pretvorbu IANA -> Windows.
        if (!TimeZoneInfo.TryConvertIanaIdToWindowsId(agency.Timezone, out _)
            || !TimeZoneInfo.TryFindSystemTimeZoneById(agency.Timezone, out _))
        {
            errors.Add(nameof(agency.Timezone), $"Nepoznata vremenska zona '{agency.Timezone}'. Koristi IANA naziv, npr. Europe/Zagreb.");
        }

        if (!IsKnownLanguage(agency.Lang))
        {
            errors.Add(nameof(agency.Lang), $"Nepoznat jezik '{agency.Lang}'. Koristi oznaku poput hr ili en.");
        }

        if (agency.Email is not null && !MailAddress.TryCreate(agency.Email, out _))
        {
            errors.Add(nameof(agency.Email), "Neispravna e-mail adresa.");
        }

        errors.ThrowIfAny();
    }

    private static bool IsKnownLanguage(string lang)
    {
        try
        {
            CultureInfo.GetCultureInfo(lang, predefinedOnly: true);
            return true;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static NotFoundException AgencyNotFound(Guid id) => new($"Agencija {id} ne postoji.");
}
