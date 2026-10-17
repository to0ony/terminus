using Microsoft.AspNetCore.Mvc;
using Terminus.Service.Common;
using Terminus.WebApi.Contracts.Variants;

namespace Terminus.WebApi.Controllers;

/// <summary>
/// Varijante linija: redoslijed stanica smjera s vremenima vožnje (glavna,
/// skraćena, iz garaže…). Predložak za nove vožnje; nije dio GTFS izvoza.
/// </summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
public class RouteVariantsController(IRouteVariantService variantService) : ControllerBase
{
    [HttpGet("routes/{routeId:guid}/variants")]
    [ProducesResponseType<IReadOnlyList<VariantResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<VariantResponse>>> List(Guid routeId, CancellationToken cancellationToken)
    {
        var variants = await variantService.ListAsync(routeId, cancellationToken);
        return Ok(variants.ToResponse());
    }

    [HttpPost("routes/{routeId:guid}/variants")]
    [ProducesResponseType<VariantResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VariantResponse>> Create(Guid routeId, VariantRequest request, CancellationToken cancellationToken)
    {
        var created = await variantService.CreateAsync(routeId, request.ToModel(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Variant.Id }, created.ToResponse());
    }

    /// <summary>
    /// Prepoznaje varijante iz vožnji linije (isti smjer i redoslijed stanica,
    /// srednja vremena vožnje) i vožnjama postavlja varijantu. 409 ako linija
    /// već ima varijante.
    /// </summary>
    [HttpPost("routes/{routeId:guid}/variants/derive")]
    [ProducesResponseType<IReadOnlyList<VariantResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IReadOnlyList<VariantResponse>>> Derive(Guid routeId, CancellationToken cancellationToken)
    {
        var variants = await variantService.DeriveAsync(routeId, cancellationToken);
        return Ok(variants.ToResponse());
    }

    [HttpGet("variants/{id:guid}")]
    [ProducesResponseType<VariantResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VariantResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var variant = await variantService.GetAsync(id, cancellationToken);
        return Ok(variant.ToResponse());
    }

    /// <summary>Zamjenjuje varijantu; stanice varijante s vožnjama ne mogu se promijeniti (409).</summary>
    [HttpPut("variants/{id:guid}")]
    [ProducesResponseType<VariantResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VariantResponse>> Update(Guid id, VariantRequest request, CancellationToken cancellationToken)
    {
        var updated = await variantService.UpdateAsync(id, request.ToModel(), cancellationToken);
        return Ok(updated.ToResponse());
    }

    /// <summary>Briše varijantu; 409 ako je koriste vožnje.</summary>
    [HttpDelete("variants/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await variantService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
