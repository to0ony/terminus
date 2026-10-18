using Microsoft.AspNetCore.Mvc;
using Terminus.Common;
using Terminus.Service.Common;
using Terminus.WebApi.Contracts.Agencies;

namespace Terminus.WebApi.Controllers;

[ApiController]
[Route("api/agencies")]
[Produces("application/json")]
public class AgenciesController(IAgencyService agencyService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<AgencyResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AgencyResponse>>> Find(
        [FromQuery] AgencyListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await agencyService.FindAsync(query.ToFilter(), cancellationToken);
        return Ok(result.Map(a => a.ToResponse()));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<AgencyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AgencyResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var agency = await agencyService.GetAsync(id, cancellationToken);
        return Ok(agency.ToResponse());
    }

    [HttpPost]
    [ProducesResponseType<AgencyResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AgencyResponse>> Create(AgencyRequest request, CancellationToken cancellationToken)
    {
        var created = await agencyService.CreateAsync(request.ToModel(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<AgencyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AgencyResponse>> Update(
        Guid id,
        AgencyRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await agencyService.UpdateAsync(id, request.ToModel(), cancellationToken);
        return Ok(updated.ToResponse());
    }

    /// <summary>
    /// Briše prijevoznika. Prijevoznik koji ima linije može se obrisati samo uz
    /// <paramref name="moveRoutesTo"/>: njegove linije i cijene prelaze na tog prijevoznika;
    /// bez toga 409.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid? moveRoutesTo, CancellationToken cancellationToken)
    {
        await agencyService.DeleteAsync(id, moveRoutesTo, cancellationToken);
        return NoContent();
    }
}
