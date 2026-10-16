using Terminus.Service.Common;
using Terminus.WebApi.Contracts.Setup;

namespace Terminus.WebApi.Contracts.Gtfs;

/// <param name="Imported">true ako su podaci upisani (samo kod uvoza).</param>
/// <param name="CanImport">true ako nema grešaka; upozorenja ne sprečavaju uvoz.</param>
/// <param name="Counts">Što se uvozi, nakon preskakanja neispravnih zapisa.</param>
/// <param name="Existing">Postojeći podaci koje uvoz briše i zamjenjuje.</param>
public record GtfsImportReportResponse(
    bool Imported,
    bool CanImport,
    GtfsImportCountsResponse Counts,
    SetupCountsResponse Existing,
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
        report.Errors.Select(ToResponse).ToList(),
        report.Warnings.Select(ToResponse).ToList(),
        report.SkippedFiles.Select(f => new GtfsSkippedFileResponse(f.File, f.Reason)).ToList());

    private static GtfsImportIssueResponse ToResponse(GtfsImportIssue issue) =>
        new(issue.File, issue.Message, issue.Count, issue.Examples);
}
