using Microsoft.AspNetCore.Mvc;
using Terminus.Common;
using Terminus.Service.Common;
using Terminus.WebApi.Contracts.Routes;

namespace Terminus.WebApi.Controllers;

[ApiController]
[Route("api/agencies/{agencyId:guid}/routes")]
[Produces("application/json")]
public class RoutesController(IRouteService routeService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<RouteResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<RouteResponse>>> Find(
        Guid agencyId,
        [FromQuery] RouteListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await routeService.FindAsync(agencyId, query.ToFilter(), cancellationToken);
        return Ok(result.Map(r => r.ToResponse()));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<RouteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RouteResponse>> Get(Guid agencyId, Guid id, CancellationToken cancellationToken)
    {
        var route = await routeService.GetAsync(agencyId, id, cancellationToken);
        return Ok(route.ToResponse());
    }

    [HttpPost]
    [ProducesResponseType<RouteResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RouteResponse>> Create(
        Guid agencyId,
        RouteRequest request,
        CancellationToken cancellationToken)
    {
        var created = await routeService.CreateAsync(agencyId, request.ToModel(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { agencyId, id = created.Id }, created.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<RouteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RouteResponse>> Update(
        Guid agencyId,
        Guid id,
        RouteRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await routeService.UpdateAsync(agencyId, id, request.ToModel(), cancellationToken);
        return Ok(updated.ToResponse());
    }

    /// <summary>Briše liniju i (kaskadno) sve njezine vožnje.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid agencyId, Guid id, CancellationToken cancellationToken)
    {
        await routeService.DeleteAsync(agencyId, id, cancellationToken);
        return NoContent();
    }
}
