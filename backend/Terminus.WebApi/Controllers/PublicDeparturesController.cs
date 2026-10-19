using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Terminus.Service.Common;
using Terminus.WebApi.Contracts.Departures;
using Terminus.WebApi.Infrastructure;

namespace Terminus.WebApi.Controllers;

/// <summary>Javni polasci za zaslone na stanicama i mobitele putnika; bez prijave.</summary>
[ApiController]
[Route("api/public/departures")]
[AllowAnonymous]
[EnableRateLimiting(PublicApi.RateLimitPolicy)]
[Produces("application/json")]
public class PublicDeparturesController(IDepartureService departureService, TimeProvider timeProvider) : ControllerBase
{
    /// <summary>
    /// Sljedeći polasci sa stanice u idućih 24 h. Za kolodvor uključuje sve perone.
    /// S Past vraća i toliko upravo otišlih polazaka (zadnjih pola sata) u Recent.
    /// Odgovor se čuva do 20 s, pa Now i MinutesUntil mogu kasniti; zaslon računa
    /// minute prema DepartureTime.
    /// </summary>
    /// <param name="stopCode">GTFS kod stanice (stop_id), npr. "OS-AK".</param>
    [HttpGet("{stopCode}")]
    [OutputCache(PolicyName = PublicApi.DeparturesCachePolicy)]
    [ProducesResponseType<DeparturesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<DeparturesResponse>> Get(
        string stopCode,
        [FromQuery] DeparturesQuery query,
        CancellationToken cancellationToken)
    {
        var result = await departureService.GetNextAsync(stopCode, timeProvider.GetUtcNow(), query.Limit, query.Past, cancellationToken);
        return Ok(result.ToResponse());
    }
}
