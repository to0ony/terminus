using Microsoft.AspNetCore.Mvc;
using Terminus.Common;
using Terminus.Service.Common;
using Terminus.WebApi.Contracts.Stops;

namespace Terminus.WebApi.Controllers;

[ApiController]
[Route("api/stops")]
[Produces("application/json")]
public class StopsController(IStopService stopService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<StopResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<StopResponse>>> Find(
        [FromQuery] StopListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await stopService.FindAsync(query.ToFilter(), cancellationToken);
        return Ok(result.Map(s => s.ToResponse()));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<StopResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StopResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var stop = await stopService.GetAsync(id, cancellationToken);
        return Ok(stop.ToResponse());
    }

    [HttpPost]
    [ProducesResponseType<StopResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StopResponse>> Create(
        StopRequest request,
        CancellationToken cancellationToken)
    {
        var created = await stopService.CreateAsync(request.ToModel(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<StopResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StopResponse>> Update(
        Guid id,
        StopRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await stopService.UpdateAsync(id, request.ToModel(), cancellationToken);
        return Ok(updated.ToResponse());
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await stopService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
