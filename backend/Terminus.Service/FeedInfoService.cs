using Terminus.Common.Exceptions;
using Terminus.Model.Common;
using Terminus.Repository.Common;
using Terminus.Service.Common;
using static Terminus.Service.Validators;

namespace Terminus.Service;

public class FeedInfoService(IFeedInfoRepository repository) : IFeedInfoService
{
    public async Task<IFeedInfoModel> GetAsync(CancellationToken cancellationToken = default) =>
        await repository.GetAsync(cancellationToken) ?? throw NotSet();

    public async Task<IFeedInfoModel> SaveAsync(IFeedInfoModel feedInfo, CancellationToken cancellationToken = default)
    {
        Normalize(feedInfo);
        Validate(feedInfo);

        return await repository.SaveAsync(feedInfo, cancellationToken);
    }

    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        if (!await repository.DeleteAsync(cancellationToken))
        {
            throw NotSet();
        }
    }

    private static void Normalize(IFeedInfoModel feedInfo)
    {
        feedInfo.PublisherName = feedInfo.PublisherName?.Trim() ?? string.Empty;
        feedInfo.PublisherUrl = feedInfo.PublisherUrl?.Trim() ?? string.Empty;
        feedInfo.Lang = feedInfo.Lang?.Trim() ?? string.Empty;
        feedInfo.DefaultLang = NullIfBlank(feedInfo.DefaultLang);
        feedInfo.ContactEmail = NullIfBlank(feedInfo.ContactEmail);
        feedInfo.ContactUrl = NullIfBlank(feedInfo.ContactUrl);
    }

    private static void Validate(IFeedInfoModel feedInfo)
    {
        var errors = new ValidationErrors();

        if (feedInfo.PublisherName.Length == 0)
        {
            errors.Add(nameof(feedInfo.PublisherName), "Naziv izdavača je obavezan.");
        }

        if (!IsHttpUrl(feedInfo.PublisherUrl))
        {
            errors.Add(nameof(feedInfo.PublisherUrl), "URL izdavača mora biti potpuna http ili https adresa.");
        }

        // GTFS dopušta i "mul" kad feed ima više jezika.
        if (feedInfo.Lang != "mul" && !IsKnownLanguage(feedInfo.Lang))
        {
            errors.Add(nameof(feedInfo.Lang), $"Nepoznat jezik '{feedInfo.Lang}'. Koristi oznaku poput hr, en ili mul.");
        }

        if (feedInfo.DefaultLang is not null && !IsKnownLanguage(feedInfo.DefaultLang))
        {
            errors.Add(nameof(feedInfo.DefaultLang), $"Nepoznat jezik '{feedInfo.DefaultLang}'.");
        }

        if (feedInfo.ContactEmail is not null && !IsEmail(feedInfo.ContactEmail))
        {
            errors.Add(nameof(feedInfo.ContactEmail), "Neispravna e-mail adresa.");
        }

        if (feedInfo.ContactUrl is not null && !IsHttpUrl(feedInfo.ContactUrl))
        {
            errors.Add(nameof(feedInfo.ContactUrl), "URL za kontakt mora biti potpuna http ili https adresa.");
        }

        errors.ThrowIfAny();
    }

    private static NotFoundException NotSet() => new("Podaci o izdavaču feeda nisu uneseni.");
}
