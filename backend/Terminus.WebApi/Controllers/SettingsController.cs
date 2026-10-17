using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Terminus.Common.Exceptions;
using Terminus.Service;
using Terminus.Service.Common;
using Terminus.WebApi.Contracts.Settings;
using Terminus.WebApi.Infrastructure;

namespace Terminus.WebApi.Controllers;

/// <summary>Postavke instance: naziv, boje i logo za javne stranice i PDF, javni URL, objava GTFS-a.</summary>
[ApiController]
[Route("api/settings")]
[Produces("application/json")]
public class SettingsController(ISettingsService settingsService, IOutputCacheStore outputCache) : ControllerBase
{
    /// <summary>Postavke; dok nisu spremljene, zadane vrijednosti.</summary>
    [HttpGet]
    [ProducesResponseType<SettingsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SettingsResponse>> Get(CancellationToken cancellationToken)
    {
        var settings = await settingsService.GetAsync(cancellationToken);
        return Ok(settings.ToResponse());
    }

    /// <summary>Zamjenjuje sve postavke osim loga.</summary>
    [HttpPut]
    [ProducesResponseType<SettingsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SettingsResponse>> Save(SettingsRequest request, CancellationToken cancellationToken)
    {
        var saved = await settingsService.SaveAsync(request.ToModel(), cancellationToken);
        await outputCache.EvictByTagAsync(PublicApi.SettingsTag, cancellationToken);
        return Ok(saved.ToResponse());
    }

    /// <summary>
    /// Postavlja logo (multipart/form-data, polje "file"): PNG, JPEG, WebP ili SVG,
    /// najviše 512 KB.
    /// </summary>
    [HttpPut("logo")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(SettingsService.MaxLogoBytes + 64 * 1024)]
    [ProducesResponseType<SettingsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SettingsResponse>> SaveLogo(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length > SettingsService.MaxLogoBytes)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Logo"] = [$"Logo smije imati najviše {SettingsService.MaxLogoBytes / 1024} KB."]
            });
        }

        using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer, cancellationToken);

        var saved = await settingsService.SaveLogoAsync(buffer.ToArray(), cancellationToken);
        await outputCache.EvictByTagAsync(PublicApi.SettingsTag, cancellationToken);
        return Ok(saved.ToResponse());
    }

    [HttpDelete("logo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLogo(CancellationToken cancellationToken)
    {
        await settingsService.DeleteLogoAsync(cancellationToken);
        await outputCache.EvictByTagAsync(PublicApi.SettingsTag, cancellationToken);
        return NoContent();
    }
}
