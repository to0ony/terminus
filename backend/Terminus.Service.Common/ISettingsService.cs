using Terminus.Model.Common;

namespace Terminus.Service.Common;

/// <summary>Postavke instance: naziv, boje, logo, javni URL i objava GTFS feeda.</summary>
public interface ISettingsService
{
    /// <returns>Spremljene postavke ili zadane (sve prazno, feed nije javan) ako još nisu spremljene.</returns>
    Task<ISettingsModel> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>Mijenja sve postavke osim loga.</summary>
    Task<ISettingsModel> SaveAsync(ISettingsModel settings, CancellationToken cancellationToken = default);

    /// <exception cref="Terminus.Common.Exceptions.NotFoundException">Logo nije postavljen.</exception>
    Task<InstanceLogo> GetLogoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Postavlja logo. Vrsta se prepoznaje iz sadržaja (PNG, JPEG, WebP ili SVG),
    /// a ne iz naziva datoteke ili zaglavlja zahtjeva.
    /// </summary>
    Task<ISettingsModel> SaveLogoAsync(byte[] content, CancellationToken cancellationToken = default);

    /// <exception cref="Terminus.Common.Exceptions.NotFoundException">Logo nije postavljen.</exception>
    Task DeleteLogoAsync(CancellationToken cancellationToken = default);

    /// <summary>Postavke za javne stranice; bez naziva instance koristi se naziv prvog prijevoznika.</summary>
    Task<PublicSettings> GetPublicAsync(CancellationToken cancellationToken = default);
}

/// <param name="DisplayName">Naziv instance, prvog prijevoznika ili null ako ni jedno nije uneseno.</param>
/// <param name="LogoUpdatedAt">Null ako logo nije postavljen.</param>
public sealed record PublicSettings(
    string? DisplayName,
    string? PrimaryColor,
    string? PrimaryTextColor,
    DateTime? LogoUpdatedAt,
    bool PublicGtfsEnabled,
    bool PublicMapEnabled);
