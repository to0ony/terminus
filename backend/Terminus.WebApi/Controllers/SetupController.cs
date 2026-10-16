using Microsoft.AspNetCore.Mvc;
using Terminus.Service.Common;
using Terminus.WebApi.Contracts.Agencies;
using Terminus.WebApi.Contracts.Setup;

namespace Terminus.WebApi.Controllers;

/// <summary>
/// Čarobnjak prvog pokretanja. Nakon prve prijave dashboard pita treba li
/// uvesti postojeći GTFS ili početi od nule; do tada instanca nema prijevoznika.
/// </summary>
[ApiController]
[Route("api/setup")]
[Produces("application/json")]
public class SetupController(ISetupService setupService) : ControllerBase
{
    /// <summary>Je li instanca postavljena i koliko podataka voznog reda ima.</summary>
    [HttpGet]
    [ProducesResponseType<SetupStatusResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SetupStatusResponse>> Get(CancellationToken cancellationToken)
    {
        var status = await setupService.GetStatusAsync(cancellationToken);
        return Ok(status.ToResponse());
    }

    /// <summary>Početak od nule: stvara prvog prijevoznika i završava čarobnjak.</summary>
    [HttpPost("blank")]
    [ProducesResponseType<AgencyResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AgencyResponse>> StartBlank(StartBlankRequest request, CancellationToken cancellationToken)
    {
        var agency = await setupService.StartBlankAsync(request.ToModel(), cancellationToken);
        return CreatedAtAction(nameof(AgenciesController.Get), "Agencies", new { id = agency.Id }, agency.ToResponse());
    }
}
