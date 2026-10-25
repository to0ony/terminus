using Terminus.Service.Common;

namespace Terminus.Service.Pdf;

/// <summary>Tekstovi PDF sheme linije na hrvatskom i engleskom.</summary>
internal abstract class RouteDiagramTexts
{
    public static RouteDiagramTexts For(TimetableLanguage language) => language switch
    {
        TimetableLanguage.En => new EnglishDiagramTexts(),
        _ => new CroatianDiagramTexts()
    };

    public abstract string FilePrefix { get; }
    public abstract string Route { get; }
    public abstract string Minutes { get; }
    public abstract string BothDirections { get; }
    public abstract string OneDirection { get; }
    public abstract string ApproximateTimes { get; }
    public abstract string OtherVariants { get; }

    /// <summary>Opis stupca minuta, npr. "↓ min od Kolodvora".</summary>
    public abstract string MinutesFrom(string from);

    public abstract string StopCount(int count);
}

internal sealed class CroatianDiagramTexts : RouteDiagramTexts
{
    public override string FilePrefix => "linija";
    public override string Route => "Linija";
    public override string Minutes => "min";
    public override string BothDirections => "staje u oba smjera";
    public override string OneDirection => "staje samo u smjeru strelice";
    public override string ApproximateTimes => "Vremena međustanica (~) su okvirna.";
    public override string OtherVariants => "Ostale varijante";

    // Naziv stanice ostaje u nominativu, pa "polazak:" umjesto "od Kolodvora".
    public override string MinutesFrom(string from) => $"min, polazak: {from}";

    public override string StopCount(int count) => count % 10 is >= 2 and <= 4 && count % 100 is < 12 or > 14
        ? $"{count} stanice"
        : $"{count} stanica";
}

internal sealed class EnglishDiagramTexts : RouteDiagramTexts
{
    public override string FilePrefix => "route";
    public override string Route => "Route";
    public override string Minutes => "min";
    public override string BothDirections => "stops in both directions";
    public override string OneDirection => "stops only in the direction of the arrow";
    public override string ApproximateTimes => "Times at intermediate stops (~) are approximate.";
    public override string OtherVariants => "Other variants";

    public override string MinutesFrom(string from) => $"min from {from}";

    public override string StopCount(int count) => count == 1 ? "1 stop" : $"{count} stops";
}
