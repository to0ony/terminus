namespace Terminus.Service;

/// <summary>
/// Neaktivno stajalište ostaje u varijantama i vožnjama, ali ga GTFS izvoz,
/// polasci i vozni red preskaču; ostale stanice zadržavaju svoja vremena.
/// Zato neaktivna stanica ne smije biti prva ni zadnja i mora ostati dovoljno
/// aktivnih stanica.
/// </summary>
internal static class SkippedStops
{
    /// <summary>Najmanje aktivnih stanica u redoslijedu koji ima neaktivnu stanicu.</summary>
    public const int MinActiveStops = 3;

    /// <summary>Koliko varijanti i vožnji poruka nabraja.</summary>
    private const int MaxListed = 5;

    public enum Problem
    {
        None,
        FirstInactive,
        LastInactive,
        TooFewActive
    }

    /// <param name="active">Je li stanica aktivna, redom vožnje.</param>
    public static Problem Check(IReadOnlyList<bool> active)
    {
        if (active.Count == 0 || active.All(a => a))
        {
            return Problem.None;
        }

        if (!active[0])
        {
            return Problem.FirstInactive;
        }

        if (!active[^1])
        {
            return Problem.LastInactive;
        }

        return active.Count(a => a) < MinActiveStops ? Problem.TooFewActive : Problem.None;
    }

    /// <summary>Greška pri unosu varijante ili vožnje; null ako redoslijed vrijedi.</summary>
    /// <param name="subject">"Vožnja" ili "Varijanta".</param>
    public static string? Message(Problem problem, string subject) => problem switch
    {
        Problem.FirstInactive => $"{subject} ne smije počinjati neaktivnom stanicom.",
        Problem.LastInactive => $"{subject} ne smije završavati neaktivnom stanicom.",
        Problem.TooFewActive => $"Neaktivne stanice se preskaču, pa bez njih moraju ostati barem {MinActiveStops} stanice.",
        _ => null
    };

    /// <summary>Popis za poruku: najviše nekoliko naziva i broj ostalih.</summary>
    public static string List(IReadOnlyList<string> names) =>
        names.Count <= MaxListed
            ? string.Join(", ", names)
            : $"{string.Join(", ", names.Take(MaxListed))} i još {names.Count - MaxListed}";
}
