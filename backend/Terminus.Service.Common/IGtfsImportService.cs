using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Service.Common;

/// <summary>
/// Uvoz osnove iz GTFS feeda (ZIP): prijevoznici, stanice, linije, trase,
/// varijante linija (prepoznate iz vožnji, s vremenima vožnje), cijene i
/// feed_info. Kalendari i vožnje se ne uvoze, nego se rade u aplikaciji.
/// Uvoz dopunjava: zapisi s istim GTFS ID-em se ažuriraju, a sezone, vožnje i
/// ručno uređene varijante ostaju.
/// </summary>
public interface IGtfsImportService
{
    /// <summary>Čita i provjerava feed bez upisa u bazu; izvještaj kaže što bi se uvezlo.</summary>
    Task<GtfsImportReport> PreviewAsync(Stream zip, CancellationToken cancellationToken = default);

    /// <summary>
    /// Isto kao pregled, a ako nema grešaka, u jednoj transakciji upisuje osnovu
    /// i završava čarobnjak prvog pokretanja. S greškama ne mijenja ništa
    /// (<see cref="GtfsImportReport.Imported"/> je false).
    /// </summary>
    /// <param name="variantKeys">Ključevi varijanti za uvoz (iz pregleda); null znači zadani izbor.</param>
    Task<GtfsImportReport> ImportAsync(Stream zip, IReadOnlyCollection<string>? variantKeys, CancellationToken cancellationToken = default);
}

/// <param name="Imported">true ako su podaci upisani (samo kod uvoza, ne kod pregleda).</param>
/// <param name="CanImport">true ako nema grešaka; upozorenja ne sprečavaju uvoz.</param>
/// <param name="Counts">Što je pročitano iz feeda (nakon preskakanja neispravnih zapisa).</param>
/// <param name="Existing">Podaci u instanci prije uvoza.</param>
/// <param name="Changes">Koliko je zapisa novo, a koliko se ažurira.</param>
/// <param name="Variants">Prepoznate varijante linija i koje se uvoze.</param>
/// <param name="Errors">Problemi zbog kojih se feed ne može uvesti.</param>
/// <param name="Warnings">Preskočeni ili ispravljeni zapisi; uvoz je moguć.</param>
/// <param name="SkippedFiles">Datoteke koje Terminus ne podržava i ne uvozi.</param>
public sealed record GtfsImportReport(
    bool Imported,
    bool CanImport,
    GtfsImportCounts Counts,
    TimetableCounts Existing,
    GtfsImportChanges Changes,
    IReadOnlyList<GtfsImportVariant> Variants,
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

/// <summary>Zapisi s GTFS ID-em koji već postoji se ažuriraju, ostali su novi.</summary>
public sealed record GtfsImportChanges(
    int NewAgencies,
    int UpdatedAgencies,
    int NewStops,
    int UpdatedStops,
    int NewRoutes,
    int UpdatedRoutes,
    int NewShapes,
    int UpdatedShapes);

/// <summary>Varijanta linije prepoznata iz vožnji feeda.</summary>
/// <param name="Key">Stalni ključ (linija, smjer i stanice) za odabir pri uvozu.</param>
/// <param name="Duration">Trajanje vožnje u sekundama po srednjim vremenima vožnje.</param>
/// <param name="Share">Udio vožnji linije u tom smjeru, 0–1.</param>
/// <param name="Main">Najčešća varijanta smjera.</param>
/// <param name="Included">Uvozi se: zadano glavne i one s barem 5 % vožnji smjera.</param>
public sealed record GtfsImportVariant(
    string Key,
    string RouteCode,
    string? RouteName,
    TripDirection? DirectionId,
    string Name,
    string? Headsign,
    int StopCount,
    int TripCount,
    int Duration,
    double Share,
    bool Main,
    bool Included);

/// <summary>Jedna vrsta problema; isti problem u više redaka broji se zajedno.</summary>
/// <param name="File">GTFS datoteka ili null za feed u cjelini.</param>
/// <param name="Count">Koliko je puta problem nađen.</param>
/// <param name="Examples">Prvih nekoliko mjesta, npr. "redak 12: stop_id 'A1'".</param>
public sealed record GtfsImportIssue(string? File, string Message, int Count, IReadOnlyList<string> Examples);

public sealed record GtfsSkippedFile(string File, string Reason);
