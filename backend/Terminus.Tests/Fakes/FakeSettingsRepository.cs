using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Repository.Common;

namespace Terminus.Tests.Fakes;

/// <summary>Postavke u memoriji.</summary>
internal sealed class FakeSettingsRepository : ISettingsRepository
{
    public ISettingsModel? Settings { get; set; }
    public InstanceLogo? Logo { get; set; }

    public Task<ISettingsModel?> GetAsync(CancellationToken cancellationToken = default)
    {
        if (Settings is not null)
        {
            Settings.LogoUpdatedAt = Logo?.UpdatedAt;
        }
        return Task.FromResult(Settings);
    }

    public Task<ISettingsModel> SaveAsync(ISettingsModel settings, CancellationToken cancellationToken = default)
    {
        Settings = settings;
        return Task.FromResult(settings);
    }

    public Task<InstanceLogo?> GetLogoAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Logo);

    public Task SaveLogoAsync(InstanceLogo logo, CancellationToken cancellationToken = default)
    {
        Settings ??= new SettingsModel();
        Logo = logo;
        return Task.CompletedTask;
    }

    public Task<bool> DeleteLogoAsync(CancellationToken cancellationToken = default)
    {
        var existed = Logo is not null;
        Logo = null;
        return Task.FromResult(existed);
    }
}
