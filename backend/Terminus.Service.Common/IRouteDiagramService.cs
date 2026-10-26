using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Service.Common;

/// <summary>Shema linije za ispis: redoslijed stanica oba smjera (PDF linije).</summary>
public interface IRouteDiagramService
{
    /// <summary>
    /// Stanice glavnih varijanti oba smjera spojene u jedan popis, redom u smjeru 0. Stanice se spajaju po nazivu, jer su stanice suprotnih
    /// smjerova obično zasebni zapisi (dvije strane ulice). 422 ako linija nema
    /// varijantu sa stanicama.
    /// </summary>
    Task<RouteDiagram> GetAsync(Guid routeId, CancellationToken cancellationToken = default);

    /// <summary>Shema linije kao PDF, s nazivom, bojama i logom iz postavki.</summary>
    Task<TimetablePdf> GetPdfAsync(Guid routeId, RouteDiagramPdfOptions options, CancellationToken cancellationToken = default);
}

/// <param name="ShowMinutes">Uz stanice minute vožnje od polazne stanice svakog smjera.</param>
public sealed record RouteDiagramPdfOptions(PaperSize Paper, TimetableLanguage Language, bool ShowMinutes = false);

/// <param name="AgencyName">Naziv prvog unesenog prijevoznika.</param>
/// <param name="Forward">Glavna varijanta smjera 0 (ili jedina).</param>
/// <param name="Backward">Glavna varijanta smjera 1, obrnutim redom; null ako je nema.</param>
/// <param name="Stops">Spojene stanice redom u smjeru 0.</param>
public sealed record RouteDiagram(
    IRouteModel Route,
    string AgencyName,
    RouteDiagramDirection Forward,
    RouteDiagramDirection? Backward,
    IReadOnlyList<RouteDiagramStop> Stops);

/// <param name="Headsign">Natpis varijante ili naziv zadnje stanice.</param>
public sealed record RouteDiagramDirection(string Headsign, string From);

/// <summary>Stanica sheme.</summary>
/// <param name="Forward">Staje li tu neka varijanta smjera 0.</param>
/// <param name="Backward">Staje li tu neka varijanta smjera 1.</param>
/// <param name="ForwardMinutes">Minute od polaska u smjeru 0 do dolaska na stanicu; null ako glavna varijanta tu ne staje.</param>
/// <param name="BackwardMinutes">Minute od polaska u smjeru 1 do dolaska na stanicu; null ako glavna varijanta tu ne staje.</param>
/// <param name="Terminus">Prva ili zadnja stanica glavne varijante nekog smjera.</param>
/// <param name="VariantTerminus">Prva ili zadnja stanica neke od ostalih varijanti, a nije <paramref name="Terminus"/>.</param>
public sealed record RouteDiagramStop(
    string Name,
    bool Forward,
    bool Backward,
    int? ForwardMinutes,
    int? BackwardMinutes,
    bool Terminus,
    bool VariantTerminus = false);
