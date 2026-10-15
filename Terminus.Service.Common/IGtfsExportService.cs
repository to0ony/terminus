namespace Terminus.Service.Common;

public interface IGtfsExportService
{
    /// <summary>
    /// Generira GTFS feed (ZIP) sa svim aktivnim linijama. Baca UnprocessableException
    /// s popisom problema ako podaci nisu spremni za izvoz.
    /// </summary>
    Task<GtfsFeedFile> ExportAsync(CancellationToken cancellationToken = default);
}

/// <summary>Generirani GTFS feed.</summary>
public sealed record GtfsFeedFile(string FileName, byte[] Content);
