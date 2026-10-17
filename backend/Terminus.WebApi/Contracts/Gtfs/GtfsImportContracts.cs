using Terminus.Model.Common.Enums;
using Terminus.Service.Common;
using Terminus.WebApi.Contracts.Setup;

namespace Terminus.WebApi.Contracts.Gtfs;

/// <param name="Imported">true ako su podaci upisani (samo kod uvoza).</param>
/// <param name="CanImport">true ako nema grešaka; upozorenja ne sprečavaju uvoz.</param>
/// <param name="Counts">Što je pročitano iz feeda, nakon preskakanja neispravnih zapisa.</param>
/// <param name="Existing">Podaci u instanci prije uvoza.</param>
/// <param name="Changes">Koliko je zapisa novo, a koliko se ažurira (isti GTFS ID).</param>
/// <param name="Variants">Prepoznate varijante linija i koje se uvoze.</param>
public record GtfsImportReportResponse(
    bool Imported,
    bool CanImport,
    GtfsImportCountsResponse Counts,
    SetupCountsResponse Existing,
    GtfsImportChangesResponse Changes,
    IReadOnlyList<GtfsImportVariantResponse> Variants,
    IReadOnlyList<GtfsImportIssueResponse> Errors,
    IReadOnlyList<GtfsImportIssueResponse> Warnings,
    IReadOnlyList<GtfsSkippedFileResponse> SkippedFiles);

/// <param name="TripsFromFrequencies">Vožnje nastale iz frequencies.txt (uključene u Trips).</param>
/// <param name="InterpolatedStopTimes">Vremena izračunata između susjednih stanica (timepoint = 0).</param>
public record GtfsImportCountsResponse(
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

public record GtfsImportChangesResponse(
    int NewAgencies,
    int UpdatedAgencies,
    int NewStops,
    int UpdatedStops,
    int NewRoutes,
    int UpdatedRoutes,
    int NewShapes,
    int UpdatedShapes);

/// <param name="Key">Ključ za odabir pri uvozu (polje "variants").</param>
/// <param name="Duration">Trajanje vožnje u sekundama.</param>
/// <param name="Share">Udio vožnji linije u tom smjeru, 0–1.</param>
/// <param name="Included">Uvozi se (zadano glavne i one s barem 5 % vožnji smjera).</param>
public record GtfsImportVariantResponse(
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

/// <param name="File">GTFS datoteka ili null za feed u cjelini.</param>
/// <param name="Count">Koliko je puta problem nađen.</param>
/// <param name="Examples">Prvih nekoliko mjesta, npr. "redak 12: stop_id 'A1'".</param>
public record GtfsImportIssueResponse(string? File, string Message, int Count, IReadOnlyList<string> Examples);

public record GtfsSkippedFileResponse(string File, string Reason);

public static class GtfsImportMappings
{
    public static GtfsImportReportResponse ToResponse(this GtfsImportReport report) => new(
        report.Imported,
        report.CanImport,
        new GtfsImportCountsResponse(
            report.Counts.Agencies,
            report.Counts.Stops,
            report.Counts.Routes,
            report.Counts.Calendars,
            report.Counts.CalendarDates,
            report.Counts.Trips,
            report.Counts.StopTimes,
            report.Counts.Shapes,
            report.Counts.ShapePoints,
            report.Counts.Fares,
            report.Counts.FareRules,
            report.Counts.FeedInfo,
            report.Counts.TripsFromFrequencies,
            report.Counts.InterpolatedStopTimes),
        new SetupCountsResponse(report.Existing.Agencies, report.Existing.Stops, report.Existing.Routes, report.Existing.Trips),
        new GtfsImportChangesResponse(
            report.Changes.NewAgencies,
            report.Changes.UpdatedAgencies,
            report.Changes.NewStops,
            report.Changes.UpdatedStops,
            report.Changes.NewRoutes,
            report.Changes.UpdatedRoutes,
            report.Changes.NewShapes,
            report.Changes.UpdatedShapes),
        report.Variants.Select(v => new GtfsImportVariantResponse(
            v.Key, v.RouteCode, v.RouteName, v.DirectionId, v.Name, v.Headsign,
            v.StopCount, v.TripCount, v.Duration, v.Share, v.Main, v.Included)).ToList(),
        report.Errors.Select(ToResponse).ToList(),
        report.Warnings.Select(ToResponse).ToList(),
        report.SkippedFiles.Select(f => new GtfsSkippedFileResponse(f.File, f.Reason)).ToList());

    private static GtfsImportIssueResponse ToResponse(GtfsImportIssue issue) =>
        new(issue.File, issue.Message, issue.Count, issue.Examples);
}
