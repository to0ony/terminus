using System.Globalization;

namespace Terminus.Common;

/// <summary>
/// Pretvorba GTFS vremena ("HH:MM:SS", sati mogu biti iznad 24) u sekunde od ponoći i obrnuto.
/// </summary>
public static class GtfsTime
{
    /// <summary>Gornja granica koju dopušta baza: 48:00:00.</summary>
    public const int MaxSeconds = 48 * 3600;

    /// <summary>
    /// Prihvaća "H:MM:SS", "HH:MM:SS" i kraći oblik "H:MM" / "HH:MM".
    /// Provjerava samo format; raspon (npr. najviše 48 h) provjerava pozivatelj.
    /// </summary>
    public static bool TryParse(string? value, out int seconds)
    {
        seconds = 0;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Trim().Split(':');
        if (parts.Length is not (2 or 3))
        {
            return false;
        }

        if (!TryParsePart(parts[0], minLength: 1, max: 99, out var hours)
            || !TryParsePart(parts[1], minLength: 2, max: 59, out var minutes))
        {
            return false;
        }

        var secs = 0;
        if (parts.Length == 3 && !TryParsePart(parts[2], minLength: 2, max: 59, out secs))
        {
            return false;
        }

        seconds = hours * 3600 + minutes * 60 + secs;
        return true;
    }

    /// <summary>Sekunde od ponoći u "HH:MM:SS" (npr. 91800 u "25:30:00").</summary>
    public static string Format(int seconds) =>
        string.Create(CultureInfo.InvariantCulture, $"{seconds / 3600:00}:{seconds % 3600 / 60:00}:{seconds % 60:00}");

    private static bool TryParsePart(string part, int minLength, int max, out int value)
    {
        value = 0;
        return part.Length >= minLength
            && part.Length <= 2
            && part.All(char.IsAsciiDigit)
            && int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out value)
            && value <= max;
    }
}
