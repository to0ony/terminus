using Microsoft.AspNetCore.Mvc;
using Terminus.Service.Common;
using Terminus.WebApi.Contracts.Gtfs;

namespace Terminus.WebApi.Controllers;

/// <summary>
/// Uvoz GTFS feeda u dva koraka: pregled (provjera i izvještaj, bez upisa) pa
/// uvoz iste datoteke. Uvoz zamjenjuje sve podatke voznog reda instance.
/// </summary>
[ApiController]
[Route("api/gtfs/import")]
[Produces("application/json")]
public class GtfsImportController(IGtfsImportService importService) : ControllerBase
{
    /// <summary>Najveći ZIP koji se prima.</summary>
    public const long MaxUploadBytes = 200L * 1024 * 1024;

    /// <summary>
    /// Čita i provjerava feed (multipart/form-data, polje "file") i vraća
    /// izvještaj: što bi se uvezlo, greške, upozorenja i preskočene datoteke.
    /// Ništa se ne upisuje.
    /// </summary>
    [HttpPost("preview")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    [ProducesResponseType<GtfsImportReportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GtfsImportReportResponse>> Preview(IFormFile file, CancellationToken cancellationToken)
    {
        await using var zip = file.OpenReadStream();
        var report = await importService.PreviewAsync(zip, cancellationToken);
        return Ok(report.ToResponse());
    }

    /// <summary>
    /// Uvozi feed: ako nema grešaka, u jednoj transakciji briše postojeći vozni
    /// red i upisuje feed te završava čarobnjak prvog pokretanja. S greškama
    /// vraća 422 i isti izvještaj, a podaci ostaju nepromijenjeni.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    [ProducesResponseType<GtfsImportReportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GtfsImportReportResponse>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<GtfsImportReportResponse>> Import(IFormFile file, CancellationToken cancellationToken)
    {
        await using var zip = file.OpenReadStream();
        var report = await importService.ImportAsync(zip, cancellationToken);

        return report.Imported
            ? Ok(report.ToResponse())
            : UnprocessableEntity(report.ToResponse());
    }
}
