using Terminus.Service.Common;

namespace Terminus.WebApi.Contracts.Routes;

/// <summary>Query parametri za PDF shemu linije.</summary>
public class RouteDiagramQuery
{
    public PaperSize Paper { get; set; } = PaperSize.A4;

    public TimetableLanguage Language { get; set; } = TimetableLanguage.Hr;

    /// <summary>Minute vožnje od polazne stanice uz svaku stanicu.</summary>
    public bool ShowMinutes { get; set; }

    /// <summary>Minute od stanice umjesto od polazne stanice; samo za PDF linija sa stanice.</summary>
    public bool MinutesFromStop { get; set; }

    public RouteDiagramPdfOptions ToOptions() => new(Paper, Language, ShowMinutes, MinutesFromStop);
}
