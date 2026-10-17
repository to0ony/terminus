using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Terminus.Common.Exceptions;
using Terminus.Service.Common;
using Terminus.WebApi.Infrastructure;

namespace Terminus.WebApi.Controllers;

/// <summary>
/// Javni GTFS feed (za Google Maps, aplikacije za planiranje putovanja i slično);
/// bez prijave. Dostupan samo ako je u postavkama uključen PublicGtfsEnabled.
/// </summary>
[ApiController]
[Route("api/public/gtfs")]
[AllowAnonymous]
[EnableRateLimiting(PublicApi.FeedRateLimitPolicy)]
public class PublicGtfsController(IGtfsExportService gtfsExportService, ISettingsService settingsService) : ControllerBase
{
    /// <summary>
    /// Isti feed kao /api/gtfs. 404 ako feed nije objavljen, 422 ako podaci
    /// trenutno nisu ispravni za izvoz.
    /// </summary>
    [HttpGet]
    [OutputCache(PolicyName = PublicApi.FeedCachePolicy)]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "application/zip")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var settings = await settingsService.GetAsync(cancellationToken);
        if (!settings.PublicGtfsEnabled)
        {
            throw new NotFoundException("GTFS feed nije javno objavljen.");
        }

        var feed = await gtfsExportService.ExportAsync(cancellationToken);
        return File(feed.Content, "application/zip", feed.FileName);
    }
}
