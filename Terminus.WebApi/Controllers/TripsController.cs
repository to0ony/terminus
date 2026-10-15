using Microsoft.AspNetCore.Mvc;
using Terminus.Common;
using Terminus.Service.Common;
using Terminus.WebApi.Contracts.Trips;

namespace Terminus.WebApi.Controllers;

[ApiController]
[Route("api/trips")]
[Produces("application/json")]
public class TripsController(ITripService tripService) : ControllerBase
{
    /// <summary>Popis vožnji, poredan po prvom polasku.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<TripResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<TripResponse>>> Find(
        [FromQuery] TripListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await tripService.FindAsync(query.ToFilter(), cancellationToken);
        return Ok(result.Map(t => t.ToResponse()));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<TripResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TripResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var trip = await tripService.GetAsync(id, cancellationToken);
        return Ok(trip.ToResponse());
    }

    [HttpPost]
    [ProducesResponseType<TripResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TripResponse>> Create(
        TripRequest request,
        CancellationToken cancellationToken)
    {
        var created = await tripService.CreateAsync(request.ToModel(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<TripResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TripResponse>> Update(
        Guid id,
        TripRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await tripService.UpdateAsync(id, request.ToModel(), cancellationToken);
        return Ok(updated.ToResponse());
    }

    /// <summary>Briše vožnju i njezina vremena.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await tripService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Stanice vožnje s vremenima, redom kojim ih vožnja obilazi.</summary>
    [HttpGet("{id:guid}/stop-times")]
    [ProducesResponseType<IReadOnlyList<StopTimeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<StopTimeResponse>>> GetStopTimes(
        Guid id,
        CancellationToken cancellationToken)
    {
        var stopTimes = await tripService.GetStopTimesAsync(id, cancellationToken);
        return Ok(stopTimes.Select(st => st.ToResponse()).ToList());
    }

    /// <summary>Zamjenjuje sve stanice i vremena vožnje odjednom.</summary>
    [HttpPut("{id:guid}/stop-times")]
    [ProducesResponseType<IReadOnlyList<StopTimeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<StopTimeResponse>>> ReplaceStopTimes(
        Guid id,
        ReplaceStopTimesRequest request,
        CancellationToken cancellationToken)
    {
        var stopTimes = await tripService.ReplaceStopTimesAsync(id, request.ToModels(), cancellationToken);
        return Ok(stopTimes.Select(st => st.ToResponse()).ToList());
    }
}
