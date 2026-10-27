namespace Terminus.Service.Common;

public interface IGtfsExportService
{
    /// <summary>
    /// Generira GTFS feed (ZIP) sa svim aktivnim linijama. Baca UnprocessableException
    /// s popisom problema ako podaci nisu spremni za izvoz.
    /// </summary>
    Task<GtfsFeedFile> ExportAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Iste provjere kao pri izvozu, bez izrade ZIP-a: popis problema zbog
    /// kojih se feed ne bi izvezao, prazan ako je spreman.
    /// </summary>
    Task<IReadOnlyList<string>> CheckAsync(CancellationToken cancellationToken = default);
}

/// <summary>Generirani GTFS feed.</summary>
public sealed record GtfsFeedFile(string FileName, byte[] Content);
