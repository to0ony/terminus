using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Terminus.Common;
using Terminus.Service.Common;
using Terminus.WebApi.Contracts.Routes;
using Terminus.WebApi.Contracts.Timetables;
using Terminus.WebApi.Infrastructure;

namespace Terminus.WebApi.Controllers;

[ApiController]
[Route("api/routes")]
[Produces("application/json")]
public class RoutesController(
    IRouteService routeService,
    ITimetableService timetableService,
    IScheduleService scheduleService,
    IOutputCacheStore outputCache,
    TimeProvider timeProvider) : ControllerBase
{
    /// <summary>
    /// Vozni red linije za grafikon: stanice smjera (redoslijed svih obrazaca, za
    /// sve kalendare isti) i vožnje s vremenima u sekundama, po želji samo
    /// jednog kalendara (tipa dana).
    /// </summary>
    [HttpGet("{id:guid}/schedule")]
    [ProducesResponseType<ScheduleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScheduleResponse>> GetSchedule(
        Guid id,
        [FromQuery] ScheduleQuery query,
        CancellationToken cancellationToken)
    {
        var schedule = await scheduleService.GetAsync(id, query.DirectionId, query.CalendarId, cancellationToken);
        return Ok(schedule.ToResponse());
    }

    /// <summary>
    /// Sprema izmjene iz grafikona u jednoj transakciji: nove vožnje (bez Id-a),
    /// izmijenjene (s UpdatedAt koji je klijent učitao) i obrisane. 409 ako je
    /// vožnju u međuvremenu promijenio netko drugi; 400 ako vremena krše GTFS
    /// pravila.
    /// </summary>
    [HttpPut("{id:guid}/schedule")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SaveSchedule(
        Guid id,
        ScheduleSaveRequest request,
        CancellationToken cancellationToken)
    {
        await scheduleService.SaveAsync(id, request.ToChanges(), cancellationToken);
        // Zasloni i javni feed odmah vide novi vozni red.
        await outputCache.EvictByTagAsync(PublicApi.TimetableTag, cancellationToken);
        return NoContent();
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<RouteResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<RouteResponse>>> Find(
        [FromQuery] RouteListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await routeService.FindAsync(query.ToFilter(), cancellationToken);
        return Ok(result.Map(r => r.ToResponse()));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<RouteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RouteResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var route = await routeService.GetAsync(id, cancellationToken);
        return Ok(route.ToResponse());
    }

    [HttpPost]
    [ProducesResponseType<RouteResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RouteResponse>> Create(
        RouteRequest request,
        CancellationToken cancellationToken)
    {
        var created = await routeService.CreateAsync(request.ToModel(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<RouteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RouteResponse>> Update(
        Guid id,
        RouteRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await routeService.UpdateAsync(id, request.ToModel(), cancellationToken);
        return Ok(updated.ToResponse());
    }

    /// <summary>
    /// Redoslijedi stanica kojima vožnje linije prolaze, po smjeru i od najčešćeg.
    /// Prvi obrazac smjera je glavni (npr. za prikaz linije na karti).
    /// </summary>
    [HttpGet("{id:guid}/patterns")]
    [ProducesResponseType<IReadOnlyList<RoutePatternResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<RoutePatternResponse>>> GetPatterns(Guid id, CancellationToken cancellationToken)
    {
        var patterns = await timetableService.GetRoutePatternsAsync(id, cancellationToken);
        return Ok(patterns.Select(p => p.ToResponse()).ToList());
    }

    /// <summary>
    /// Vozni red linije kao tablica stanica × vožnji, za dan voznog reda (date)
    /// ili jedan kalendar (serviceCalendarId); bez oboje za današnji dan.
    /// </summary>
    [HttpGet("{id:guid}/timetable")]
    [ProducesResponseType<RouteTimetableResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RouteTimetableResponse>> GetTimetable(
        Guid id,
        [FromQuery] TimetableQuery query,
        CancellationToken cancellationToken)
    {
        var timetable = await timetableService.GetRouteTimetableAsync(id, query.ToQuery(), timeProvider.GetUtcNow(), cancellationToken);
        return Ok(timetable.ToResponse());
    }

    /// <summary>Briše liniju i (kaskadno) sve njezine vožnje.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await routeService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
