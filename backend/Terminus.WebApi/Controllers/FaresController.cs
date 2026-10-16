using Microsoft.AspNetCore.Mvc;
using Terminus.Common;
using Terminus.Service.Common;
using Terminus.WebApi.Contracts.Fares;

namespace Terminus.WebApi.Controllers;

[ApiController]
[Route("api/fares")]
[Produces("application/json")]
public class FaresController(IFareService fareService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<FareResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<FareResponse>>> Find(
        [FromQuery] FareListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await fareService.FindAsync(query.ToFilter(), cancellationToken);
        return Ok(result.Map(f => f.ToResponse()));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<FareResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FareResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var fare = await fareService.GetAsync(id, cancellationToken);
        return Ok(fare.ToResponse());
    }

    [HttpPost]
    [ProducesResponseType<FareResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FareResponse>> Create(FareRequest request, CancellationToken cancellationToken)
    {
        var created = await fareService.CreateAsync(request.ToModel(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created.ToResponse());
    }

    /// <summary>Mijenja cijenu i zamjenjuje sva njezina pravila.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<FareResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FareResponse>> Update(Guid id, FareRequest request, CancellationToken cancellationToken)
    {
        var updated = await fareService.UpdateAsync(id, request.ToModel(), cancellationToken);
        return Ok(updated.ToResponse());
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await fareService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
