namespace Terminus.Service.Common;

/// <summary>Vozni red linije jednog dana kao Excel tablica.</summary>
public interface IRouteTimetableExcelService
{
    /// <summary>
    /// Vožnje linije zadanog dana (bez njega današnji u zoni prijevoznika), svaki
    /// smjer na svom listu: stanice su redci, vožnje stupci. 422 ako linija taj
    /// dan ne vozi.
    /// </summary>
    Task<TimetableFile> GetXlsxAsync(
        Guid routeId,
        DateOnly? date,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}

public sealed record TimetableFile(byte[] Content, string FileName);
