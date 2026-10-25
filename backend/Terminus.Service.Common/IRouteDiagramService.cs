using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Service.Common;

/// <summary>Shema linije za ispis: redoslijed stanica oba smjera (PDF linije).</summary>
public interface IRouteDiagramService
{
    /// <summary>
    /// Stanice glavnih varijanti oba smjera spojene u jedan popis, odozgo prema
    /// dolje u smjeru 0. Stanice se spajaju po nazivu, jer su stanice suprotnih
    /// smjerova obično zasebni zapisi (dvije strane ulice). 422 ako linija nema
    /// varijantu sa stanicama.
    /// </summary>
    Task<RouteDiagram> GetAsync(Guid routeId, CancellationToken cancellationToken = default);

    /// <summary>Shema linije kao PDF, s nazivom, bojama i logom iz postavki.</summary>
    Task<TimetablePdf> GetPdfAsync(Guid routeId, RouteDiagramPdfOptions options, CancellationToken cancellationToken = default);
}

public sealed record RouteDiagramPdfOptions(PaperSize Paper, TimetableLanguage Language);

/// <param name="AgencyName">Naziv prvog unesenog prijevoznika.</param>
/// <param name="Forward">Glavna varijanta smjera 0 (ili jedina), odozgo prema dolje.</param>
/// <param name="Backward">Glavna varijanta smjera 1, odozdo prema gore; null ako je nema.</param>
/// <param name="Stops">Spojene stanice odozgo prema dolje.</param>
/// <param name="OtherVariants">Ostale varijante, za legendu.</param>
public sealed record RouteDiagram(
    IRouteModel Route,
    string AgencyName,
    RouteDiagramDirection Forward,
    RouteDiagramDirection? Backward,
    IReadOnlyList<RouteDiagramStop> Stops,
    IReadOnlyList<RouteDiagramVariant> OtherVariants);

/// <param name="Headsign">Natpis varijante ili naziv zadnje stanice.</param>
public sealed record RouteDiagramDirection(string Headsign, string From);

/// <summary>Stanica sheme; vrijeme je null u smjeru koji tu ne staje.</summary>
/// <param name="ForwardMinutes">Minute od polaska u smjeru 0 do dolaska na stanicu.</param>
/// <param name="BackwardMinutes">Minute od polaska u smjeru 1 do dolaska na stanicu.</param>
/// <param name="Terminus">Prva ili zadnja stanica glavne varijante nekog smjera.</param>
/// <param name="VariantTerminus">Prva ili zadnja stanica neke od ostalih varijanti, a nije <paramref name="Terminus"/>.</param>
public sealed record RouteDiagramStop(string Name, int? ForwardMinutes, int? BackwardMinutes, bool Terminus, bool VariantTerminus = false)
{
    public bool Forward => ForwardMinutes is not null;
    public bool Backward => BackwardMinutes is not null;
}

/// <param name="Range">Retci sheme kojima varijanta prolazi; null ako nijednim.</param>
public sealed record RouteDiagramVariant(
    string Name,
    TripDirection? DirectionId,
    string From,
    string To,
    int StopCount,
    RouteDiagramRange? Range = null);

/// <summary>Raspon redaka sheme, kao interval: zatvoren kraj je i kraj varijante.</summary>
/// <param name="TopClosed">Varijanta počinje ili završava u retku <paramref name="Top"/>.</param>
/// <param name="BottomClosed">Varijanta počinje ili završava u retku <paramref name="Bottom"/>.</param>
public sealed record RouteDiagramRange(int Top, int Bottom, bool TopClosed, bool BottomClosed);
