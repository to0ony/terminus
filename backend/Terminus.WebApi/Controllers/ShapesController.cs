using Microsoft.AspNetCore.Mvc;
using Terminus.Common;
using Terminus.Service.Common;
using Terminus.WebApi.Contracts.Shapes;

namespace Terminus.WebApi.Controllers;

[ApiController]
[Route("api/shapes")]
[Produces("application/json")]
public class ShapesController(IShapeService shapeService) : ControllerBase
{
    /// <summary>Popis trasa bez točaka.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<ShapeSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ShapeSummaryResponse>>> Find(
        [FromQuery] ShapeListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await shapeService.FindAsync(query.ToFilter(), cancellationToken);
        return Ok(result.Map(s => s.ToSummaryResponse()));
    }

    /// <summary>Trasa sa svim točkama.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ShapeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShapeResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var shape = await shapeService.GetAsync(id, cancellationToken);
        return Ok(shape.ToResponse());
    }

    [HttpPost]
    [ProducesResponseType<ShapeResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ShapeResponse>> Create(ShapeRequest request, CancellationToken cancellationToken)
    {
        var created = await shapeService.CreateAsync(request.ToModel(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created.ToResponse());
    }

    /// <summary>Mijenja kod i zamjenjuje sve točke trase.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<ShapeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ShapeResponse>> Update(Guid id, ShapeRequest request, CancellationToken cancellationToken)
    {
        var updated = await shapeService.UpdateAsync(id, request.ToModel(), cancellationToken);
        return Ok(updated.ToResponse());
    }

    /// <summary>Briše trasu; vožnje koje su je koristile ostaju bez trase.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await shapeService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
