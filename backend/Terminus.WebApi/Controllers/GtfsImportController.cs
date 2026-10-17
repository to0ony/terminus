using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Terminus.Common.Exceptions;
using Terminus.Service.Common;
using Terminus.WebApi.Contracts.Gtfs;
using Terminus.WebApi.Infrastructure;

namespace Terminus.WebApi.Controllers;

/// <summary>
/// Uvoz osnove iz GTFS feeda u dva koraka: pregled (provjera, izvještaj i
/// prepoznate varijante linija, bez upisa) pa uvoz iste datoteke. Uvoze se
/// prijevoznici, stanice, linije, trase, varijante, cijene i feed_info kao
/// dopuna; kalendari i vožnje se rade u aplikaciji.
/// </summary>
[ApiController]
[Route("api/gtfs/import")]
[Produces("application/json")]
public class GtfsImportController(IGtfsImportService importService, IOutputCacheStore outputCache) : ControllerBase
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
    /// Uvozi osnovu: ako nema grešaka, u jednoj transakciji ažurira zapise s
    /// istim GTFS ID-em i dodaje nove (sezone, vožnje i ručne varijante ostaju)
    /// te završava čarobnjak prvog pokretanja. Polje "variants" (JSON popis
    /// ključeva iz pregleda) bira varijante; bez njega vrijedi zadani izbor. S
    /// greškama vraća 422 i isti izvještaj, a podaci ostaju nepromijenjeni.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    [ProducesResponseType<GtfsImportReportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GtfsImportReportResponse>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<GtfsImportReportResponse>> Import(
        IFormFile file,
        [FromForm] string? variants,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<string>? variantKeys;
        try
        {
            variantKeys = variants is null ? null : JsonSerializer.Deserialize<List<string>>(variants);
        }
        catch (JsonException)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Variants"] = ["Varijante se šalju kao JSON popis ključeva."]
            });
        }

        await using var zip = file.OpenReadStream();
        var report = await importService.ImportAsync(zip, variantKeys, cancellationToken);

        if (report.Imported)
        {
            // Javni polasci i feed inače bi još neko vrijeme pokazivali stari vozni red.
            await outputCache.EvictByTagAsync(PublicApi.TimetableTag, cancellationToken);
        }

        return report.Imported
            ? Ok(report.ToResponse())
            : UnprocessableEntity(report.ToResponse());
    }
}
