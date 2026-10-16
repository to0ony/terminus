using Terminus.Model.Common;

namespace Terminus.Repository.Common;

/// <summary>Jedini zapis postavki instance.</summary>
public interface ISettingsRepository
{
    /// <returns>Postavke bez sadržaja loga, ili null ako još nisu spremljene.</returns>
    Task<ISettingsModel?> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>Stvara zapis ili mijenja postojeći; logo ostaje kakav je.</summary>
    Task<ISettingsModel> SaveAsync(ISettingsModel settings, CancellationToken cancellationToken = default);

    /// <returns>Logo ili null ako nije postavljen.</returns>
    Task<InstanceLogo?> GetLogoAsync(CancellationToken cancellationToken = default);

    /// <summary>Postavlja logo; ako postavke ne postoje, stvara ih sa zadanim vrijednostima.</summary>
    Task SaveLogoAsync(InstanceLogo logo, CancellationToken cancellationToken = default);

    /// <returns>false ako logo nije bio postavljen.</returns>
    Task<bool> DeleteLogoAsync(CancellationToken cancellationToken = default);
}
