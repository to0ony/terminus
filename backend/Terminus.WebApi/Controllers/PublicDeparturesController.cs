using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Terminus.Service.Common;
using Terminus.WebApi.Contracts.Departures;

namespace Terminus.WebApi.Controllers;

/// <summary>Javni polasci za zaslone na stanicama i mobitele putnika; bez prijave.</summary>
[ApiController]
[Route("api/public/departures")]
[AllowAnonymous]
[Produces("application/json")]
public class PublicDeparturesController(IDepartureService departureService, TimeProvider timeProvider) : ControllerBase
{
    /// <summary>
    /// Sljedeći polasci sa stanice u idućih 24 h. Za kolodvor uključuje sve perone.
    /// </summary>
    /// <param name="stopCode">GTFS kod stanice (stop_id), npr. "OS-AK".</param>
    [HttpGet("{stopCode}")]
    [ProducesResponseType<DeparturesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<DeparturesResponse>> Get(
        string stopCode,
        [FromQuery] DeparturesQuery query,
        CancellationToken cancellationToken)
    {
        var result = await departureService.GetNextAsync(stopCode, timeProvider.GetUtcNow(), query.Limit, cancellationToken);
        return Ok(result.ToResponse());
    }
}
