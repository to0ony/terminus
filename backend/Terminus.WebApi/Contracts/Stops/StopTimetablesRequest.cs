using System.ComponentModel.DataAnnotations;
using Terminus.Service.Common;

namespace Terminus.WebApi.Contracts.Stops;

/// <summary>Tijelo zahtjeva za PDF voznih redova više stanica.</summary>
public class StopTimetablesRequest
{
    /// <summary>Stanice redom kojim idu u PDF (najviše 100).</summary>
    [Required]
    public List<Guid>? Ids { get; set; }

    /// <summary>Prvi dan razdoblja; bez njega danas.</summary>
    public DateOnly? From { get; set; }

    /// <summary>Zadnji dan razdoblja; bez njega zadnji dan s voznim redom.</summary>
    public DateOnly? To { get; set; }

    public PaperSize Paper { get; set; } = PaperSize.A4;

    public TimetableLanguage Language { get; set; } = TimetableLanguage.Hr;

    public TimetablePdfOptions ToOptions() => new(From, To, Paper, Language);
}
