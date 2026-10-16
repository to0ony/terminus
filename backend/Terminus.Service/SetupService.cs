using Terminus.Common;
using Terminus.Common.Exceptions;
using Terminus.Model.Common;
using Terminus.Repository.Common;
using Terminus.Service.Common;

namespace Terminus.Service;

public class SetupService(
    ISetupRepository setupRepository,
    ISettingsRepository settingsRepository,
    IAgencyService agencyService,
    TimeProvider timeProvider) : ISetupService
{
    public async Task<SetupStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var completedAt = (await settingsRepository.GetAsync(cancellationToken))?.SetupCompletedAt;
        var counts = await setupRepository.GetCountsAsync(cancellationToken);

        return new SetupStatus(completedAt is not null, completedAt, counts);
    }

    public async Task<IAgencyModel> StartBlankAsync(IAgencyModel agency, CancellationToken cancellationToken = default)
    {
        if ((await settingsRepository.GetAsync(cancellationToken))?.SetupCompletedAt is not null)
        {
            throw AlreadyCompleted();
        }

        if (string.IsNullOrWhiteSpace(agency.Code) && !string.IsNullOrWhiteSpace(agency.Name))
        {
            agency.Code = Slug.From(agency.Name, fallback: "1");
        }

        // Prijevoznik se provjerava i sprema kao kroz /api/agencies (zadana zona i jezik, ista pravila).
        var created = await agencyService.CreateAsync(agency, cancellationToken);
        await settingsRepository.MarkSetupCompletedAsync(timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

        return created;
    }

    private static ConflictException AlreadyCompleted() =>
        new("Instanca je već postavljena; novi prijevoznici se dodaju kroz /api/agencies.");
}
