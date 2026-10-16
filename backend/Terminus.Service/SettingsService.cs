using System.Text;
using Terminus.Common.Exceptions;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Repository.Common;
using Terminus.Service.Common;
using static Terminus.Service.Validators;

namespace Terminus.Service;

public class SettingsService(
    ISettingsRepository repository,
    IAgencyRepository agencyRepository,
    TimeProvider timeProvider) : ISettingsService
{
    /// <summary>Logo se šalje na svaku javnu stranicu i u PDF, pa mora biti malen.</summary>
    public const int MaxLogoBytes = 512 * 1024;

    public const int MaxDisplayNameLength = 100;

    public async Task<ISettingsModel> GetAsync(CancellationToken cancellationToken = default) =>
        await repository.GetAsync(cancellationToken) ?? new SettingsModel();

    public async Task<ISettingsModel> SaveAsync(ISettingsModel settings, CancellationToken cancellationToken = default)
    {
        Normalize(settings);
        Validate(settings);

        return await repository.SaveAsync(settings, cancellationToken);
    }

    public async Task<InstanceLogo> GetLogoAsync(CancellationToken cancellationToken = default) =>
        await repository.GetLogoAsync(cancellationToken) ?? throw LogoNotSet();

    public async Task<ISettingsModel> SaveLogoAsync(byte[] content, CancellationToken cancellationToken = default)
    {
        var errors = new ValidationErrors();

        var contentType = DetectImageType(content);
        if (content.Length == 0)
        {
            errors.Add("Logo", "Datoteka je prazna.");
        }
        else if (content.Length > MaxLogoBytes)
        {
            errors.Add("Logo", $"Logo smije imati najviše {MaxLogoBytes / 1024} KB.");
        }
        else if (contentType is null)
        {
            errors.Add("Logo", "Logo mora biti slika u formatu PNG, JPEG, WebP ili SVG.");
        }

        errors.ThrowIfAny();

        await repository.SaveLogoAsync(
            new InstanceLogo(content, contentType!, timeProvider.GetUtcNow().UtcDateTime),
            cancellationToken);

        return await GetAsync(cancellationToken);
    }

    public async Task DeleteLogoAsync(CancellationToken cancellationToken = default)
    {
        if (!await repository.DeleteLogoAsync(cancellationToken))
        {
            throw LogoNotSet();
        }
    }

    public async Task<PublicSettings> GetPublicAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetAsync(cancellationToken);

        var displayName = settings.DisplayName
            ?? (await agencyRepository.GetFirstAsync(cancellationToken))?.Name;

        return new PublicSettings(
            displayName,
            settings.PrimaryColor,
            settings.PrimaryTextColor,
            settings.LogoUpdatedAt,
            settings.PublicGtfsEnabled);
    }

    /// <returns>MIME vrsta slike prema potpisu na početku datoteke, ili null.</returns>
    internal static string? DetectImageType(ReadOnlySpan<byte> content)
    {
        if (content.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]))
        {
            return "image/png";
        }

        if (content.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF]))
        {
            return "image/jpeg";
        }

        if (content.Length >= 12 && content[..4].SequenceEqual("RIFF"u8) && content[8..12].SequenceEqual("WEBP"u8))
        {
            return "image/webp";
        }

        return IsSvg(Encoding.UTF8.GetString(content[..Math.Min(content.Length, 4096)])) ? "image/svg+xml" : null;
    }

    /// <summary>SVG je tekst: prije elementa svg smiju doći samo XML deklaracija, komentari i DOCTYPE.</summary>
    private static bool IsSvg(string text)
    {
        var rest = text.AsSpan().TrimStart('﻿').TrimStart();

        while (true)
        {
            if (rest.StartsWith("<svg", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var end = rest.StartsWith("<?") ? rest.IndexOf("?>") + 2
                : rest.StartsWith("<!--") ? rest.IndexOf("-->") + 3
                : rest.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ? rest.IndexOf('>') + 1
                : 0;

            // Bez poznatog početka ili bez kraja (indeks -1 + n < n) nije SVG.
            if (end <= 2)
            {
                return false;
            }

            rest = rest[end..].TrimStart();
        }
    }

    private static void Normalize(ISettingsModel settings)
    {
        settings.DisplayName = NullIfBlank(settings.DisplayName);
        settings.PrimaryColor = NormalizeColor(settings.PrimaryColor);
        settings.PrimaryTextColor = NormalizeColor(settings.PrimaryTextColor);
        settings.PublicUrl = NullIfBlank(settings.PublicUrl)?.TrimEnd('/');
    }

    private static void Validate(ISettingsModel settings)
    {
        var errors = new ValidationErrors();

        if (settings.DisplayName is { Length: > MaxDisplayNameLength })
        {
            errors.Add(nameof(settings.DisplayName), $"Naziv smije imati najviše {MaxDisplayNameLength} znakova.");
        }

        if (settings.PrimaryColor is not null && !IsHexColor(settings.PrimaryColor))
        {
            errors.Add(nameof(settings.PrimaryColor), "Boja mora biti hex vrijednost od 6 znakova, npr. 1565C0.");
        }

        if (settings.PrimaryTextColor is not null && !IsHexColor(settings.PrimaryTextColor))
        {
            errors.Add(nameof(settings.PrimaryTextColor), "Boja teksta mora biti hex vrijednost od 6 znakova, npr. FFFFFF.");
        }

        if (settings.PublicUrl is not null && !IsHttpUrl(settings.PublicUrl))
        {
            errors.Add(nameof(settings.PublicUrl), "Javna adresa mora biti potpuna http ili https adresa.");
        }

        errors.ThrowIfAny();
    }

    private static NotFoundException LogoNotSet() => new("Logo nije postavljen.");
}
