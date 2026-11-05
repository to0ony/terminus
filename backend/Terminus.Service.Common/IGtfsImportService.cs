using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Service.Common;

/// <summary>
/// Uvoz GTFS feeda (ZIP) koji zamjenjuje sve podatke voznog reda: prijevoznike,
/// stanice, linije, trase, varijante linija (prepoznate iz vožnji), kalendare,
/// vožnje s vremenima, cijene i feed_info. Postojeće sezone se brišu, a iz
/// kalendara feeda može nastati nova. Korisnici i postavke instance ostaju.
/// </summary>
public interface IGtfsImportService
{
    /// <summary>Čita i provjerava feed bez upisa u bazu; izvještaj kaže što bi se uvezlo.</summary>
    Task<GtfsImportReport> PreviewAsync(Stream zip, CancellationToken cancellationToken = default);

    /// <summary>
    /// Isto kao pregled, a ako nema grešaka, u jednoj transakciji briše postojeće
    /// podatke voznog reda, upisuje feed i završava čarobnjak prvog pokretanja. S
    /// greškama ne mijenja ništa (<see cref="GtfsImportReport.Imported"/> je false).
    /// </summary>
    Task<GtfsImportReport> ImportAsync(Stream zip, GtfsImportOptions options, CancellationToken cancellationToken = default);
}

/// <param name="VariantKeys">
/// Ključevi varijanti za uvoz (iz pregleda); null znači sve. Vožnje neodabranih
/// varijanti se ne uvoze.
/// </param>
/// <param name="SkipExpired">Izostavlja kalendare koji od današnjeg dana više ne voze i njihove vožnje.</param>
/// <param name="CreateSeason">
/// Pretvara kalendare u sezonu ako je to moguće; inače ostaju izvan sezona uz
/// upozorenje. Uz <paramref name="SkipExpired"/> sezona počinje današnjim danom.
/// </param>
public sealed record GtfsImportOptions(
    IReadOnlyCollection<string>? VariantKeys = null,
    bool SkipExpired = false,
    bool CreateSeason = false);

/// <param name="Imported">true ako su podaci upisani (samo kod uvoza, ne kod pregleda).</param>
/// <param name="CanImport">true ako nema grešaka; upozorenja ne sprečavaju uvoz.</param>
/// <param name="Counts">Što se uvozi (nakon preskakanja neispravnih zapisa i primjene odabira).</param>
/// <param name="Existing">Podaci u instanci prije uvoza; uvoz ih briše.</param>
/// <param name="Expired">Dio feeda koji od današnjeg dana više ne vozi.</param>
/// <param name="Season">Sezona koja bi nastala iz cijelog feeda; null ako feed ima greške.</param>
/// <param name="ActiveSeason">Sezona bez isteklih kalendara, od današnjeg dana; null ako feed ima greške.</param>
/// <param name="Variants">Prepoznate varijante linija i koje se uvoze.</param>
/// <param name="Errors">Problemi zbog kojih se feed ne može uvesti.</param>
/// <param name="Warnings">Preskočeni ili ispravljeni zapisi; uvoz je moguć.</param>
/// <param name="SkippedFiles">Datoteke koje Terminus ne podržava i ne uvozi.</param>
public sealed record GtfsImportReport(
    bool Imported,
    bool CanImport,
    GtfsImportCounts Counts,
    TimetableCounts Existing,
    GtfsImportExpired Expired,
    GtfsImportSeason? Season,
    GtfsImportSeason? ActiveSeason,
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

/// <summary>Kalendari bez ijednog dana vožnje od danas nadalje i njihove vožnje.</summary>
public sealed record GtfsImportExpired(int Calendars, int Trips, int StopTimes);

/// <summary>Sezona koja bi nastala iz kalendara feeda.</summary>
/// <param name="Problem">Zašto se ne može napraviti; null ako može.</param>
/// <param name="DayTypes">Nazivi tipova dana.</param>
/// <param name="Exceptions">Broj dana koji odstupaju od dana u tjednu.</param>
public sealed record GtfsImportSeason(
    string? Problem,
    string? Name,
    DateOnly? StartDate,
    DateOnly? EndDate,
    IReadOnlyList<string> DayTypes,
    int Exceptions);

/// <summary>Varijanta linije prepoznata iz vožnji feeda.</summary>
/// <param name="Key">Stalni ključ (linija, smjer i stanice) za odabir pri uvozu.</param>
/// <param name="Duration">Trajanje vožnje u sekundama po srednjim vremenima vožnje.</param>
/// <param name="Share">Udio vožnji linije u tom smjeru, 0–1.</param>
/// <param name="ExpiredTripCount">Koliko je od <paramref name="TripCount"/> vožnji u kalendarima koji više ne voze.</param>
/// <param name="Main">Najčešća varijanta smjera.</param>
/// <param name="Included">Uvozi se; zadano sve varijante, jer bi se inače izgubile njihove vožnje.</param>
public sealed record GtfsImportVariant(
    string Key,
    string RouteCode,
    string? RouteName,
    TripDirection? DirectionId,
    string Name,
    string? Headsign,
    int StopCount,
    int TripCount,
    int ExpiredTripCount,
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
