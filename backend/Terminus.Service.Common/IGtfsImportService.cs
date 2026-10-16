using Terminus.Model.Common;

namespace Terminus.Service.Common;

/// <summary>
/// Uvoz GTFS feeda (ZIP). Uvoz zamjenjuje sve podatke voznog reda instance
/// (prijevoznici, stanice, linije, kalendari, vožnje, trase, cijene, feed_info);
/// postavke instance i administratori ostaju.
/// </summary>
public interface IGtfsImportService
{
    /// <summary>Čita i provjerava feed bez upisa u bazu; izvještaj kaže što bi se uvezlo.</summary>
    Task<GtfsImportReport> PreviewAsync(Stream zip, CancellationToken cancellationToken = default);

    /// <summary>
    /// Isto kao pregled, a ako nema grešaka, u jednoj transakciji zamjenjuje
    /// podatke i završava čarobnjak prvog pokretanja. S greškama ne mijenja ništa
    /// (<see cref="GtfsImportReport.Imported"/> je false).
    /// </summary>
    Task<GtfsImportReport> ImportAsync(Stream zip, CancellationToken cancellationToken = default);
}

/// <param name="Imported">true ako su podaci upisani (samo kod uvoza, ne kod pregleda).</param>
/// <param name="CanImport">true ako nema grešaka; upozorenja ne sprečavaju uvoz.</param>
/// <param name="Counts">Što se uvozi (nakon preskakanja neispravnih zapisa).</param>
/// <param name="Existing">Podaci koje uvoz zamjenjuje, stanje prije uvoza.</param>
/// <param name="Errors">Problemi zbog kojih se feed ne može uvesti.</param>
/// <param name="Warnings">Preskočeni ili ispravljeni zapisi; uvoz je moguć.</param>
/// <param name="SkippedFiles">Datoteke koje Terminus ne podržava i ne uvozi.</param>
public sealed record GtfsImportReport(
    bool Imported,
    bool CanImport,
    GtfsImportCounts Counts,
    TimetableCounts Existing,
    IReadOnlyList<GtfsImportIssue> Errors,
    IReadOnlyList<GtfsImportIssue> Warnings,
    IReadOnlyList<GtfsSkippedFile> SkippedFiles);

/// <param name="TripsFromFrequencies">Vožnje nastale iz frequencies.txt (uključene u Trips).</param>
/// <param name="InterpolatedStopTimes">Vremena koja feed nije imao, izračunata između susjednih (timepoint = 0).</param>
public sealed record GtfsImportCounts(
    int Agencies,
    int Stops,
    int Routes,
    int Calendars,
    int CalendarDates,
    int Trips,
    int StopTimes,
    int Shapes,
    int ShapePoints,
    int Fares,
    int FareRules,
    bool FeedInfo,
    int TripsFromFrequencies,
    int InterpolatedStopTimes);

/// <summary>Jedna vrsta problema; isti problem u više redaka broji se zajedno.</summary>
/// <param name="File">GTFS datoteka ili null za feed u cjelini.</param>
/// <param name="Count">Koliko je puta problem nađen.</param>
/// <param name="Examples">Prvih nekoliko mjesta, npr. "redak 12: stop_id 'A1'".</param>
public sealed record GtfsImportIssue(string? File, string Message, int Count, IReadOnlyList<string> Examples);

public sealed record GtfsSkippedFile(string File, string Reason);
