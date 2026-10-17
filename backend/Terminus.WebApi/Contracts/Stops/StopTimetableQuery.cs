using Terminus.Service.Common;

namespace Terminus.WebApi.Contracts.Stops;

/// <summary>Query parametri za PDF vozni red stanice.</summary>
public class StopTimetableQuery
{
    /// <summary>Prvi dan razdoblja; bez njega danas.</summary>
    public DateOnly? From { get; set; }

    /// <summary>Zadnji dan razdoblja; bez njega zadnji dan s voznim redom.</summary>
    public DateOnly? To { get; set; }

    public PaperSize Paper { get; set; } = PaperSize.A4;

    public TimetableLanguage Language { get; set; } = TimetableLanguage.Hr;

    public TimetablePdfOptions ToOptions() => new(From, To, Paper, Language);
}
