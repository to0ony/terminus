namespace Terminus.Model.Common;

/// <summary>
/// Postavke instance za javne stranice stanica i PDF vozne redove. Logo se
/// sprema i čita zasebno (<see cref="InstanceLogo"/>).
/// </summary>
public interface ISettingsModel
{
    /// <summary>Naziv na javnim stranicama; bez njega se koristi naziv prvog prijevoznika.</summary>
    string? DisplayName { get; set; }

    /// <summary>Glavna boja (hex bez #, npr. "1565C0").</summary>
    string? PrimaryColor { get; set; }

    /// <summary>Boja teksta na glavnoj boji.</summary>
    string? PrimaryTextColor { get; set; }

    /// <summary>Javna adresa aplikacije (npr. "https://voznired.gpp.hr"), za QR kodove u PDF-u.</summary>
    string? PublicUrl { get; set; }

    /// <summary>Je li GTFS feed javno dostupan na /api/public/gtfs.</summary>
    bool PublicGtfsEnabled { get; set; }

    /// <summary>Je li karta uživo javno dostupna (/karta i /api/public/map).</summary>
    bool PublicMapEnabled { get; set; }

    /// <summary>Vrijeme zadnje promjene loga ili null ako logo nije postavljen. Samo za čitanje.</summary>
    DateTime? LogoUpdatedAt { get; set; }

    /// <summary>Kad je instanca postavljena (uvoz GTFS-a ili početak od nule); null dok nije. Samo za čitanje.</summary>
    DateTime? SetupCompletedAt { get; set; }

    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}

/// <summary>Logo instance.</summary>
/// <param name="ContentType">MIME vrsta, npr. "image/png".</param>
public sealed record InstanceLogo(byte[] Content, string ContentType, DateTime UpdatedAt);
