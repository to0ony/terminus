using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Service.Common;

namespace Terminus.WebApi.Contracts.Settings;

/// <summary>Postavke instance. Sva polja su neobavezna; PUT zamjenjuje sve osim loga.</summary>
public class SettingsRequest
{
    /// <summary>Naziv na javnim stranicama; bez njega se koristi naziv prvog prijevoznika.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Glavna boja, hex s # ili bez, npr. "#1565C0".</summary>
    public string? PrimaryColor { get; set; }

    /// <summary>Boja teksta na glavnoj boji.</summary>
    public string? PrimaryTextColor { get; set; }

    /// <summary>Javna adresa aplikacije, npr. "https://voznired.gpp.hr" (za QR kodove).</summary>
    public string? PublicUrl { get; set; }

    /// <summary>Objavi GTFS feed bez prijave na /api/public/gtfs.</summary>
    public bool PublicGtfsEnabled { get; set; }
}

/// <param name="LogoUrl">Adresa loga ili null ako nije postavljen.</param>
/// <param name="CreatedAt">Null dok postavke nisu prvi put spremljene.</param>
public record SettingsResponse(
    string? DisplayName,
    string? PrimaryColor,
    string? PrimaryTextColor,
    string? PublicUrl,
    bool PublicGtfsEnabled,
    string? LogoUrl,
    DateTime? CreatedAt,
    DateTime? UpdatedAt);

/// <summary>Postavke za javne stranice stanica.</summary>
/// <param name="DisplayName">Naziv instance ili prvog prijevoznika.</param>
/// <param name="LogoUrl">Adresa loga ili null ako nije postavljen.</param>
/// <param name="GtfsUrl">Adresa javnog GTFS feeda ili null ako nije objavljen.</param>
public record PublicSettingsResponse(
    string? DisplayName,
    string? PrimaryColor,
    string? PrimaryTextColor,
    string? LogoUrl,
    string? GtfsUrl);

public static class SettingsMappings
{
    public const string PublicLogoPath = "/api/public/settings/logo";
    public const string PublicGtfsPath = "/api/public/gtfs";

    public static ISettingsModel ToModel(this SettingsRequest request) => new SettingsModel
    {
        DisplayName = request.DisplayName,
        PrimaryColor = request.PrimaryColor,
        PrimaryTextColor = request.PrimaryTextColor,
        PublicUrl = request.PublicUrl,
        PublicGtfsEnabled = request.PublicGtfsEnabled
    };

    public static SettingsResponse ToResponse(this ISettingsModel model) => new(
        model.DisplayName,
        model.PrimaryColor,
        model.PrimaryTextColor,
        model.PublicUrl,
        model.PublicGtfsEnabled,
        LogoUrl(model.LogoUpdatedAt),
        model.CreatedAt == default ? null : model.CreatedAt,
        model.UpdatedAt == default ? null : model.UpdatedAt);

    public static PublicSettingsResponse ToResponse(this PublicSettings settings) => new(
        settings.DisplayName,
        settings.PrimaryColor,
        settings.PrimaryTextColor,
        LogoUrl(settings.LogoUpdatedAt),
        settings.PublicGtfsEnabled ? PublicGtfsPath : null);

    /// <summary>Verzija u adresi mijenja se s logom, pa ga preglednik smije dugo čuvati.</summary>
    private static string? LogoUrl(DateTime? updatedAt) =>
        updatedAt is { } at ? $"{PublicLogoPath}?v={at.Ticks}" : null;
}
