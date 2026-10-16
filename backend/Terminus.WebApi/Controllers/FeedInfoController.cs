using Microsoft.AspNetCore.Mvc;
using Terminus.Service.Common;
using Terminus.WebApi.Contracts.FeedInfo;

namespace Terminus.WebApi.Controllers;

/// <summary>
/// Izdavač GTFS feeda (feed_info.txt). Dok nije unesen, izvoz kao izdavača
/// koristi prvog prijevoznika.
/// </summary>
[ApiController]
[Route("api/feed-info")]
[Produces("application/json")]
public class FeedInfoController(IFeedInfoService feedInfoService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<FeedInfoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeedInfoResponse>> Get(CancellationToken cancellationToken)
    {
        var feedInfo = await feedInfoService.GetAsync(cancellationToken);
        return Ok(feedInfo.ToResponse());
    }

    /// <summary>Stvara ili mijenja podatke o izdavaču.</summary>
    [HttpPut]
    [ProducesResponseType<FeedInfoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FeedInfoResponse>> Save(FeedInfoRequest request, CancellationToken cancellationToken)
    {
        var saved = await feedInfoService.SaveAsync(request.ToModel(), cancellationToken);
        return Ok(saved.ToResponse());
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(CancellationToken cancellationToken)
    {
        await feedInfoService.DeleteAsync(cancellationToken);
        return NoContent();
    }
}
