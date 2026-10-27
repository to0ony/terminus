using Terminus.Common.Exceptions;
using Terminus.Service;
using Terminus.Service.Common;
using Terminus.Tests.Fakes;

namespace Terminus.Tests.Service;

public class StopPrintServiceTests
{
    private static readonly TimetablePdfOptions Timetable = new(null, null, PaperSize.A4, TimetableLanguage.Hr);
    private static readonly RouteDiagramPdfOptions Routes = new(PaperSize.A4, TimetableLanguage.Hr);

    // Provjere zahtjeva idu prije svakog dohvaćanja, pa servisi PDF-a nisu potrebni.
    private readonly StopPrintService _service = new(new FakeStopRepository([]), null!, null!);

    [Fact]
    public async Task GetPdfAsync_NoStops_IsRejected()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.GetPdfAsync([], new StopPrintOptions(true, false, Timetable, Routes), DateTimeOffset.UtcNow, TestContext.Current.CancellationToken));

        Assert.Contains("Ids", ex.Errors.Keys);
    }

    [Fact]
    public async Task GetPdfAsync_TooManyStops_IsRejected()
    {
        var ids = Enumerable.Range(0, StopPrintService.MaxStops + 1).Select(_ => Guid.NewGuid()).ToList();

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.GetPdfAsync(ids, new StopPrintOptions(true, true, Timetable, Routes), DateTimeOffset.UtcNow, TestContext.Current.CancellationToken));

        Assert.Contains("Ids", ex.Errors.Keys);
    }

    [Fact]
    public async Task GetPdfAsync_NeitherTimetableNorRoutes_IsRejected()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.GetPdfAsync([Guid.NewGuid()], new StopPrintOptions(false, false, Timetable, Routes), DateTimeOffset.UtcNow, TestContext.Current.CancellationToken));

        Assert.Contains("Timetable", ex.Errors.Keys);
    }

    [Fact]
    public async Task GetPdfAsync_UnknownStop_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetPdfAsync([Guid.NewGuid()], new StopPrintOptions(true, false, Timetable, Routes), DateTimeOffset.UtcNow, TestContext.Current.CancellationToken));
    }
}
