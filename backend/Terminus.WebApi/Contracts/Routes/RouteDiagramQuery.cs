using Terminus.Service.Common;

namespace Terminus.WebApi.Contracts.Routes;

/// <summary>Query parametri za PDF shemu linije.</summary>
public class RouteDiagramQuery
{
    public PaperSize Paper { get; set; } = PaperSize.A4;

    public TimetableLanguage Language { get; set; } = TimetableLanguage.Hr;

    public RouteDiagramPdfOptions ToOptions() => new(Paper, Language);
}
