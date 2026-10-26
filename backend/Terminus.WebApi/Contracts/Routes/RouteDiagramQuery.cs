using Terminus.Service.Common;

namespace Terminus.WebApi.Contracts.Routes;

/// <summary>Query parametri za PDF shemu linije.</summary>
public class RouteDiagramQuery
{
    public PaperSize Paper { get; set; } = PaperSize.A4;

    public TimetableLanguage Language { get; set; } = TimetableLanguage.Hr;

    /// <summary>Minute vožnje od polazne stanice uz svaku stanicu.</summary>
    public bool ShowMinutes { get; set; }

    public RouteDiagramPdfOptions ToOptions() => new(Paper, Language, ShowMinutes);
}
