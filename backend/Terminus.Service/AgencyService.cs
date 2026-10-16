using Terminus.Common;
using Terminus.Common.Exceptions;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;
using Terminus.Repository.Common;
using Terminus.Service.Common;
using static Terminus.Service.Validators;

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
        await ValidateAsync(agency, cancellationToken);

        return await repository.InsertAsync(agency, cancellationToken);
    }

    public async Task<IAgencyModel> UpdateAsync(Guid id, IAgencyModel agency, CancellationToken cancellationToken = default)
    {
        agency.Id = id;
        Normalize(agency);
        await ValidateAsync(agency, cancellationToken);

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
        agency.Code = agency.Code?.Trim() ?? string.Empty;
        agency.Name = agency.Name?.Trim() ?? string.Empty;
        agency.Url = agency.Url?.Trim() ?? string.Empty;
        agency.Timezone = NullIfBlank(agency.Timezone) ?? DefaultTimezone;
        agency.Lang = NullIfBlank(agency.Lang) ?? DefaultLang;
        agency.Phone = NullIfBlank(agency.Phone);
        agency.FareUrl = NullIfBlank(agency.FareUrl);
        agency.Email = NullIfBlank(agency.Email);
    }

    private async Task ValidateAsync(IAgencyModel agency, CancellationToken cancellationToken)
    {
        var errors = new ValidationErrors();

        // GTFS: svi prijevoznici u feedu moraju imati istu zonu; polasci se računaju u njoj.
        if (await repository.GetOtherTimezoneAsync(agency.Id, agency.Timezone, cancellationToken) is { } otherTimezone)
        {
            errors.Add(nameof(agency.Timezone), $"Svi prijevoznici moraju imati istu vremensku zonu; postojeći koriste '{otherTimezone}'.");
        }

        if (agency.Code.Length == 0)
        {
            errors.Add(nameof(agency.Code), "Kod je obavezan.");
        }

        if (agency.Name.Length == 0)
        {
            errors.Add(nameof(agency.Name), "Naziv je obavezan.");
        }

        // GTFS agency.txt traži agency_url.
        if (agency.Url.Length == 0)
        {
            errors.Add(nameof(agency.Url), "URL je obavezan.");
        }
        else if (!IsHttpUrl(agency.Url))
        {
            errors.Add(nameof(agency.Url), "URL mora biti potpuna http ili https adresa.");
        }

        if (agency.FareUrl is not null && !IsHttpUrl(agency.FareUrl))
        {
            errors.Add(nameof(agency.FareUrl), "URL cijena mora biti potpuna http ili https adresa.");
        }

        if (!IsIanaTimezone(agency.Timezone))
        {
            errors.Add(nameof(agency.Timezone), $"Nepoznata vremenska zona '{agency.Timezone}'. Koristi IANA naziv, npr. Europe/Zagreb.");
        }

        if (!IsKnownLanguage(agency.Lang))
        {
            errors.Add(nameof(agency.Lang), $"Nepoznat jezik '{agency.Lang}'. Koristi oznaku poput hr ili en.");
        }

        if (agency.Email is not null && !IsEmail(agency.Email))
        {
            errors.Add(nameof(agency.Email), "Neispravna e-mail adresa.");
        }

        errors.ThrowIfAny();
    }

    private static NotFoundException AgencyNotFound(Guid id) => new($"Agencija {id} ne postoji.");
}
