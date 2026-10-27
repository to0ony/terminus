namespace Terminus.Service.Common;

/// <summary>Ispis stanica: vozni redovi i sheme linija više stanica u jednom PDF-u.</summary>
public interface IStopPrintService
{
    /// <summary>
    /// Za svaku stanicu, redom kojim su zadane, vozni red i/ili sheme linija
    /// koje na njoj staju, svaki od nove stranice. Stanica bez ičega za ispis
    /// (bez polazaka ni linija) se preskače; ako takve su sve, 422.
    /// </summary>
    Task<StopPrintPdf> GetPdfAsync(
        IReadOnlyCollection<Guid> stopIds,
        StopPrintOptions options,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}

/// <param name="Timetable">Vozni red stanice (razdoblje i jezik iz <paramref name="TimetableOptions"/>).</param>
/// <param name="Routes">Sheme linija sa stanice (minute iz <paramref name="RouteOptions"/>).</param>
public sealed record StopPrintOptions(
    bool Timetable,
    bool Routes,
    TimetablePdfOptions TimetableOptions,
    RouteDiagramPdfOptions RouteOptions);

/// <param name="Skipped">Nazivi stanica bez ičega za ispis, koje nisu u PDF-u.</param>
public sealed record StopPrintPdf(byte[] Content, string FileName, IReadOnlyList<string> Skipped);
