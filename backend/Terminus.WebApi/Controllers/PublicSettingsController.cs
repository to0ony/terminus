using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Terminus.Service.Common;
using Terminus.WebApi.Contracts.Settings;

namespace Terminus.WebApi.Controllers;

/// <summary>Naziv, boje i logo za javne stranice stanica; bez prijave.</summary>
[ApiController]
[Route("api/public/settings")]
[AllowAnonymous]
[Produces("application/json")]
public class PublicSettingsController(ISettingsService settingsService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PublicSettingsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PublicSettingsResponse>> Get(CancellationToken cancellationToken)
    {
        var settings = await settingsService.GetPublicAsync(cancellationToken);
        return Ok(settings.ToResponse());
    }

    /// <summary>
    /// Logo. Adresa iz postavki sadrži verziju (?v=), pa se odgovor smije čuvati
    /// godinu dana; ETag pokriva dohvat bez verzije.
    /// </summary>
    [HttpGet("logo")]
    [Produces("image/png", "image/jpeg", "image/webp", "image/svg+xml")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLogo([FromQuery(Name = "v")] long? version, CancellationToken cancellationToken)
    {
        var logo = await settingsService.GetLogoAsync(cancellationToken);
        var etag = new EntityTagHeaderValue($"\"{logo.UpdatedAt.Ticks}\"");

        Response.Headers.CacheControl = version == logo.UpdatedAt.Ticks
            ? "public, max-age=31536000, immutable"
            : "public, no-cache";

        // SVG može sadržavati skripte; otvoren izravno ne smije ništa izvršiti.
        Response.Headers.ContentSecurityPolicy = "default-src 'none'; style-src 'unsafe-inline'; sandbox";
        Response.Headers.XContentTypeOptions = "nosniff";

        return File(logo.Content, logo.ContentType, lastModified: null, entityTag: etag);
    }
}
