using System.Globalization;
using System.Text;

namespace Terminus.Common;

/// <summary>Pretvara tekst u oblik za nazive datoteka i kodove: mala ASCII slova, znamenke i crtice.</summary>
public static class Slug
{
    /// <summary>npr. "Gradski prijevoz Đakovo d.o.o." u "gradski-prijevoz-dakovo-d-o-o".</summary>
    /// <param name="fallback">Vraća se kad u tekstu nema nijednog slova ni znamenke.</param>
    public static string From(string value, string fallback)
    {
        // Đ/đ se ne rastavlja normalizacijom kao č, ć, š, ž.
        var normalized = value.Replace('Đ', 'D').Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var slug = new StringBuilder();

        foreach (var c in normalized)
        {
            if (char.IsAsciiLetterOrDigit(c))
            {
                slug.Append(char.ToLowerInvariant(c));
            }
            else if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark
                     && slug.Length > 0 && slug[^1] != '-')
            {
                slug.Append('-');
            }
        }

        var result = slug.ToString().Trim('-');
        return result.Length > 0 ? result : fallback;
    }
}
