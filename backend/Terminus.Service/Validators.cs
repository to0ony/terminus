using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Terminus.Service;

/// <summary>Provjere formata zajedničke više servisa.</summary>
internal static partial class Validators
{
    /// <returns>true za potpunu http ili https adresu.</returns>
    public static bool IsHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    /// <summary>
    /// GTFS traži IANA naziv. TryFindSystemTimeZoneById bi na Windowsu prihvatio i
    /// Windows nazive ("Central European Standard Time"), pa se provjerava i pretvorba IANA -> Windows.
    /// </summary>
    public static bool IsIanaTimezone(string value) =>
        TimeZoneInfo.TryConvertIanaIdToWindowsId(value, out _)
        && TimeZoneInfo.TryFindSystemTimeZoneById(value, out _);

    public static bool IsKnownLanguage(string value)
    {
        try
        {
            CultureInfo.GetCultureInfo(value, predefinedOnly: true);
            return true;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    public static bool IsEmail(string value) => MailAddress.TryCreate(value, out _);

    public static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>"#ff0000" postaje "FF0000" (GTFS traži hex bez #).</summary>
    public static string? NormalizeColor(string? value) =>
        NullIfBlank(value)?.TrimStart('#').ToUpperInvariant();

    /// <returns>true za hex boju od 6 znakova bez #, velikim slovima (nakon <see cref="NormalizeColor"/>).</returns>
    public static bool IsHexColor(string value) => HexColorRegex().IsMatch(value);

    [GeneratedRegex("^[0-9A-F]{6}$")]
    private static partial Regex HexColorRegex();
}
