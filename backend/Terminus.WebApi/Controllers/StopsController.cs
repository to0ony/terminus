using Microsoft.AspNetCore.Mvc;
using Terminus.Common;
using Terminus.Service.Common;
using Terminus.WebApi.Contracts.Routes;
using Terminus.WebApi.Contracts.Stops;
using Terminus.WebApi.Contracts.Timetables;

namespace Terminus.WebApi.Controllers;

[ApiController]
[Route("api/stops")]
[Produces("application/json")]
public class StopsController(
    IStopService stopService,
    ITimetableService timetableService,
    IDepartureService departureService,
    IStopTimetableService stopTimetableService,
    TimeProvider timeProvider) : ControllerBase
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

    /// <summary>
    /// Stanice s koordinatama unutar područja karte, najviše 2000. Ako ih ima
    /// više, truncated je true i kartu treba približiti.
    /// </summary>
    [HttpGet("area")]
    [ProducesResponseType<StopsInAreaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StopsInAreaResponse>> FindInArea(
        [FromQuery] StopAreaQuery query,
        CancellationToken cancellationToken)
    {
        var result = await stopService.FindInAreaAsync(query.ToBounds(), query.IsActive, cancellationToken);
        return Ok(result.ToResponse());
    }

    /// <summary>
    /// Područje koje obuhvaća sve stanice s koordinatama, za početni prikaz
    /// karte; 204 ako nijedna stanica nema koordinate.
    /// </summary>
    [HttpGet("extent")]
    [ProducesResponseType<GeoBoundsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<GeoBoundsResponse>> GetExtent(CancellationToken cancellationToken)
    {
        var extent = await stopService.GetExtentAsync(cancellationToken);
        return extent is null ? NoContent() : Ok(extent.ToResponse());
    }

    /// <summary>Linije koje staju na stanici; za kolodvor na bilo kojem peronu.</summary>
    [HttpGet("{id:guid}/routes")]
    [ProducesResponseType<IReadOnlyList<RouteResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<RouteResponse>>> GetRoutes(Guid id, CancellationToken cancellationToken)
    {
        var routes = await timetableService.GetStopRoutesAsync(id, cancellationToken);
        return Ok(routes.Select(r => r.ToResponse()).ToList());
    }

    /// <summary>
    /// Svi polasci sa stanice jednog dana voznog reda (za kolodvor sa svih
    /// perona), prema kalendarima koji tog dana voze. Vožnje iza ponoći
    /// pripadaju danu na koji su krenule.
    /// </summary>
    [HttpGet("{id:guid}/departures")]
    [ProducesResponseType<StopDayDeparturesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StopDayDeparturesResponse>> GetDepartures(
        Guid id,
        [FromQuery] StopDayDeparturesQuery query,
        CancellationToken cancellationToken)
    {
        var result = await departureService.GetForDayAsync(id, query.Date, timeProvider.GetUtcNow(), cancellationToken);
        return Ok(result.ToResponse());
    }

    /// <summary>
    /// Vozni red stanice za ispis (PDF): polasci po liniji i smjeru, stupci po
    /// danima u tjednu s istim voznim redom, odstupanja kao napomene. Razdoblje
    /// je zadano od danas do zadnjeg dana s voznim redom, najviše 366 dana.
    /// 422 ako u razdoblju sa stanice nema polazaka.
    /// </summary>
    [HttpGet("{id:guid}/timetable.pdf")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "application/pdf")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetTimetablePdf(
        Guid id,
        [FromQuery] StopTimetableQuery query,
        CancellationToken cancellationToken)
    {
        var pdf = await stopTimetableService.GetPdfAsync(id, query.ToOptions(), timeProvider.GetUtcNow(), cancellationToken);
        return File(pdf.Content, "application/pdf", pdf.FileName);
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
