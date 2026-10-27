using Terminus.Common.Exceptions;
using Terminus.Service;
using Terminus.Service.Common;
using Terminus.Tests.Fakes;

namespace Terminus.Tests.Service;

public class StopTimetableServiceTests
{
    private static readonly TimetablePdfOptions Options = new(null, null, PaperSize.A4, TimetableLanguage.Hr);

    // Provjere broja stanica idu prije svakog dohvaćanja, pa ostali servisi nisu potrebni.
    private readonly StopTimetableService _service = new(
        new FakeStopRepository([]),
        null!,
        null!,
        null!,
        null!);

    [Fact]
    public async Task GetPdfManyAsync_NoStops_IsRejected()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.GetPdfManyAsync([], Options, DateTimeOffset.UtcNow, TestContext.Current.CancellationToken));

        Assert.Contains("Ids", ex.Errors.Keys);
    }

    [Fact]
    public async Task GetPdfManyAsync_TooManyStops_IsRejected()
    {
        var ids = Enumerable.Range(0, StopTimetableService.MaxPdfStops + 1).Select(_ => Guid.NewGuid()).ToList();

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.GetPdfManyAsync(ids, Options, DateTimeOffset.UtcNow, TestContext.Current.CancellationToken));

        Assert.Contains("Ids", ex.Errors.Keys);
    }
}
