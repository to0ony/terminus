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
    public abstract string StopFilePrefix { get; }
    public abstract string ThisStop { get; }
    public abstract string Route { get; }
    public abstract string Minutes { get; }
    public abstract string Terminus { get; }
    public abstract string OneDirection { get; }
    public abstract string ApproximateTimes { get; }

    /// <summary>Opis minuta uz stanice, npr. "min, polazak: Kolodvor".</summary>
    public abstract string MinutesFrom(string from);

    /// <summary>Opis minuta od istaknute stanice, npr. "min od ove stanice, smjer Retfala".</summary>
    public abstract string MinutesTowards(string headsign);

    public abstract string StopCount(int count);
}

internal sealed class CroatianDiagramTexts : RouteDiagramTexts
{
    public override string FilePrefix => "linija";
    public override string StopFilePrefix => "linije-stanica";
    public override string ThisStop => "ova stanica";
    public override string Route => "Linija";
    public override string Minutes => "min";
    public override string Terminus => "početna ili završna stanica";
    public override string OneDirection => "staje samo u smjeru strelice";
    public override string ApproximateTimes => "Minute uz međustanice (~) su okvirne.";

    // Naziv stanice ostaje u nominativu, pa "polazak:" umjesto "od Kolodvora".
    public override string MinutesFrom(string from) => $"min, polazak: {from}";

    public override string MinutesTowards(string headsign) => $"min od ove stanice, smjer {headsign}";

    public override string StopCount(int count) => count % 10 is >= 2 and <= 4 && count % 100 is < 12 or > 14
        ? $"{count} stanice"
        : $"{count} stanica";
}

internal sealed class EnglishDiagramTexts : RouteDiagramTexts
{
    public override string FilePrefix => "route";
    public override string StopFilePrefix => "routes-stop";
    public override string ThisStop => "this stop";
    public override string Route => "Route";
    public override string Minutes => "min";
    public override string Terminus => "first or last stop";
    public override string OneDirection => "stops only in the direction of the arrow";
    public override string ApproximateTimes => "Times at intermediate stops (~) are approximate.";

    public override string MinutesFrom(string from) => $"min from {from}";

    public override string MinutesTowards(string headsign) => $"min from this stop towards {headsign}";

    public override string StopCount(int count) => count == 1 ? "1 stop" : $"{count} stops";
}
