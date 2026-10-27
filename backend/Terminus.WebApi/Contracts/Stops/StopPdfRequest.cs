using System.ComponentModel.DataAnnotations;
using Terminus.Service.Common;

namespace Terminus.WebApi.Contracts.Stops;

/// <summary>Tijelo zahtjeva za PDF jedne ili više stanica: vozni red, sheme linija ili oboje.</summary>
public class StopPdfRequest
{
    /// <summary>Stanice redom kojim idu u PDF (najviše 100).</summary>
    [Required]
    public List<Guid>? Ids { get; set; }

    /// <summary>Vozni red stanice.</summary>
    public bool Timetable { get; set; } = true;

    /// <summary>Sheme linija koje staju na stanici.</summary>
    public bool Routes { get; set; }

    /// <summary>Prvi dan voznog reda; bez njega danas.</summary>
    public DateOnly? From { get; set; }

    /// <summary>Zadnji dan voznog reda; bez njega zadnji dan s voznim redom.</summary>
    public DateOnly? To { get; set; }

    public PaperSize Paper { get; set; } = PaperSize.A4;

    public TimetableLanguage Language { get; set; } = TimetableLanguage.Hr;

    /// <summary>Na shemama minute vožnje uz stanice.</summary>
    public bool ShowMinutes { get; set; }

    /// <summary>Na shemama minute od stanice umjesto od polazne stanice.</summary>
    public bool MinutesFromStop { get; set; }

    public StopPrintOptions ToOptions() => new(
        Timetable,
        Routes,
        new TimetablePdfOptions(From, To, Paper, Language),
        new RouteDiagramPdfOptions(Paper, Language, ShowMinutes, MinutesFromStop));
}
