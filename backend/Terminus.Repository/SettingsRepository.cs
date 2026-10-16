using Microsoft.EntityFrameworkCore;
using Npgsql;
using Terminus.Common.Exceptions;
using Terminus.DAL;
using Terminus.DAL.Entities;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Repository.Common;

namespace Terminus.Repository;

/// <summary>
/// Postavke se čitaju projekcijom bez stupca logo, da se slika ne učitava kod
/// svakog dohvata. Izmjene idu kroz ExecuteUpdate iz istog razloga; on ne prolazi
/// kroz AuditableInterceptor, pa se UpdatedAt postavlja ovdje.
/// </summary>
public class SettingsRepository(TerminusDbContext context, TimeProvider timeProvider) : ISettingsRepository
{
    public Task<ISettingsModel?> GetAsync(CancellationToken cancellationToken = default) =>
        context.Settings
            .AsNoTracking()
            .Where(s => s.Id == InstanceSettings.SingletonId)
            .Select(s => (ISettingsModel?)new SettingsModel
            {
                DisplayName = s.DisplayName,
                PrimaryColor = s.PrimaryColor,
                PrimaryTextColor = s.PrimaryTextColor,
                PublicUrl = s.PublicUrl,
                PublicGtfsEnabled = s.PublicGtfsEnabled,
                LogoUpdatedAt = s.LogoUpdatedAt,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ISettingsModel> SaveAsync(ISettingsModel settings, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var updated = await context.Settings
            .Where(s => s.Id == InstanceSettings.SingletonId)
            .ExecuteUpdateAsync(set => set
                .SetProperty(s => s.DisplayName, settings.DisplayName)
                .SetProperty(s => s.PrimaryColor, settings.PrimaryColor)
                .SetProperty(s => s.PrimaryTextColor, settings.PrimaryTextColor)
                .SetProperty(s => s.PublicUrl, settings.PublicUrl)
                .SetProperty(s => s.PublicGtfsEnabled, settings.PublicGtfsEnabled)
                .SetProperty(s => s.UpdatedAt, now),
                cancellationToken);

        if (updated == 0)
        {
            await InsertAsync(new InstanceSettings
            {
                DisplayName = settings.DisplayName,
                PrimaryColor = settings.PrimaryColor,
                PrimaryTextColor = settings.PrimaryTextColor,
                PublicUrl = settings.PublicUrl,
                PublicGtfsEnabled = settings.PublicGtfsEnabled
            }, cancellationToken);
        }

        return (await GetAsync(cancellationToken))!;
    }

    public Task<InstanceLogo?> GetLogoAsync(CancellationToken cancellationToken = default) =>
        context.Settings
            .AsNoTracking()
            .Where(s => s.Id == InstanceSettings.SingletonId && s.Logo != null)
            .Select(s => new InstanceLogo(s.Logo!, s.LogoContentType!, s.LogoUpdatedAt!.Value))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task SaveLogoAsync(InstanceLogo logo, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var updated = await context.Settings
            .Where(s => s.Id == InstanceSettings.SingletonId)
            .ExecuteUpdateAsync(set => set
                .SetProperty(s => s.Logo, logo.Content)
                .SetProperty(s => s.LogoContentType, logo.ContentType)
                .SetProperty(s => s.LogoUpdatedAt, logo.UpdatedAt)
                .SetProperty(s => s.UpdatedAt, now),
                cancellationToken);

        if (updated == 0)
        {
            await InsertAsync(new InstanceSettings
            {
                Logo = logo.Content,
                LogoContentType = logo.ContentType,
                LogoUpdatedAt = logo.UpdatedAt
            }, cancellationToken);
        }
    }

    public async Task<bool> DeleteLogoAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var updated = await context.Settings
            .Where(s => s.Id == InstanceSettings.SingletonId && s.Logo != null)
            .ExecuteUpdateAsync(set => set
                .SetProperty(s => s.Logo, (byte[]?)null)
                .SetProperty(s => s.LogoContentType, (string?)null)
                .SetProperty(s => s.LogoUpdatedAt, (DateTime?)null)
                .SetProperty(s => s.UpdatedAt, now),
                cancellationToken);

        return updated > 0;
    }

    private async Task InsertAsync(InstanceSettings entity, CancellationToken cancellationToken)
    {
        context.Settings.Add(entity);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is PostgresException { ConstraintName: "pk_settings" })
        {
            // Netko je istovremeno stvorio zapis.
            throw new ConflictException("Postavke su upravo spremljene. Pokušaj ponovno.", ex);
        }
        finally
        {
            context.Entry(entity).State = EntityState.Detached;
        }
    }
}
