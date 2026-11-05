using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Terminus.Common.Exceptions;
using Terminus.Service.Common;
using Terminus.WebApi.Contracts.Gtfs;
using Terminus.WebApi.Infrastructure;

namespace Terminus.WebApi.Controllers;

/// <summary>
/// Uvoz GTFS feeda u dva koraka: pregled (provjera, izvještaj i prepoznate
/// varijante linija, bez upisa) pa uvoz iste datoteke. Uvoz zamjenjuje sve
/// podatke voznog reda, uključujući sezone i vožnje rađene u aplikaciji.
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
    /// Uvozi feed: ako nema grešaka, u jednoj transakciji briše sve podatke
    /// voznog reda (korisnici i postavke ostaju), upisuje feed i završava čarobnjak
    /// prvog pokretanja. Polje "variants" (JSON popis ključeva iz pregleda) bira
    /// varijante i njihove vožnje; bez njega se uvoze sve. Polje "skipExpired"
    /// izostavlja kalendare koji više ne voze. S greškama vraća 422 i isti
    /// izvještaj, a podaci ostaju nepromijenjeni.
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
        [FromForm] bool skipExpired,
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
        var report = await importService.ImportAsync(zip, new GtfsImportOptions(variantKeys, skipExpired), cancellationToken);

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
