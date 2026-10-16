using Terminus.Repository.Common;
using Terminus.Service.Common;

namespace Terminus.Service.Gtfs.Import;

public class GtfsImportService(
    IGtfsImportRepository importRepository,
    ISetupRepository setupRepository,
    ISettingsRepository settingsRepository,
    TimeProvider timeProvider) : IGtfsImportService
{
    public async Task<GtfsImportReport> PreviewAsync(Stream zip, CancellationToken cancellationToken = default)
    {
        var (report, _) = await ReadAsync(zip, cancellationToken);
        return report;
    }

    public async Task<GtfsImportReport> ImportAsync(Stream zip, CancellationToken cancellationToken = default)
    {
        var (report, feed) = await ReadAsync(zip, cancellationToken);
        if (feed is null)
        {
            return report;
        }

        await importRepository.ReplaceAllAsync(feed, cancellationToken);
        await settingsRepository.MarkSetupCompletedAsync(timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

        return report with { Imported = true };
    }

    private async Task<(GtfsImportReport Report, Model.Common.GtfsFeedData? Feed)> ReadAsync(Stream zip, CancellationToken cancellationToken)
    {
        var existing = await setupRepository.GetCountsAsync(cancellationToken);
        var builder = new ImportReportBuilder();

        Model.Common.GtfsFeedData? feed = null;
        var counts = new GtfsImportCounts(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, false, 0, 0);

        using (var archive = GtfsArchive.Open(zip, out var error))
        {
            if (archive is null)
            {
                builder.Error(null, error!);
            }
            else
            {
                // Čitanje i provjera su CPU posao nad cijelim feedom; ne blokira dretvu zahtjeva.
                (feed, counts) = await Task.Run(() => GtfsFeedReader.Read(archive, builder), cancellationToken);
            }
        }

        var report = new GtfsImportReport(
            Imported: false,
            CanImport: !builder.HasErrors,
            Counts: counts,
            Existing: existing,
            Errors: builder.Errors,
            Warnings: builder.Warnings,
            SkippedFiles: builder.SkippedFiles);

        return (report, feed);
    }
}
