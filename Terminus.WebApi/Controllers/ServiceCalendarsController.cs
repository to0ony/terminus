using Microsoft.AspNetCore.Mvc;
using Terminus.Common;
using Terminus.Service.Common;
using Terminus.WebApi.Contracts.Calendars;

namespace Terminus.WebApi.Controllers;

[ApiController]
[Route("api/agencies/{agencyId:guid}/calendars")]
[Produces("application/json")]
public class ServiceCalendarsController(IServiceCalendarService calendarService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<ServiceCalendarResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ServiceCalendarResponse>>> Find(
        Guid agencyId,
        [FromQuery] ServiceCalendarListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await calendarService.FindAsync(agencyId, query.ToFilter(), cancellationToken);
        return Ok(result.Map(c => c.ToResponse()));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ServiceCalendarResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceCalendarResponse>> Get(Guid agencyId, Guid id, CancellationToken cancellationToken)
    {
        var calendar = await calendarService.GetAsync(agencyId, id, cancellationToken);
        return Ok(calendar.ToResponse());
    }

    [HttpPost]
    [ProducesResponseType<ServiceCalendarResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ServiceCalendarResponse>> Create(
        Guid agencyId,
        ServiceCalendarRequest request,
        CancellationToken cancellationToken)
    {
        var created = await calendarService.CreateAsync(agencyId, request.ToModel(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { agencyId, id = created.Id }, created.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<ServiceCalendarResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ServiceCalendarResponse>> Update(
        Guid agencyId,
        Guid id,
        ServiceCalendarRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await calendarService.UpdateAsync(agencyId, id, request.ToModel(), cancellationToken);
        return Ok(updated.ToResponse());
    }

    /// <summary>Briše kalendar i njegove iznimke. Ne uspijeva (409) ako ga koriste vožnje.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid agencyId, Guid id, CancellationToken cancellationToken)
    {
        await calendarService.DeleteAsync(agencyId, id, cancellationToken);
        return NoContent();
    }

    /// <summary>Sve iznimke kalendara, poredane po datumu.</summary>
    [HttpGet("{id:guid}/dates")]
    [ProducesResponseType<IReadOnlyList<CalendarDateResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CalendarDateResponse>>> GetDates(
        Guid agencyId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var dates = await calendarService.GetDatesAsync(agencyId, id, cancellationToken);
        return Ok(dates.Select(d => d.ToResponse()).ToList());
    }

    /// <summary>Dodaje iznimku za datum (yyyy-MM-dd) ili mijenja postojeću.</summary>
    [HttpPut("{id:guid}/dates/{date}")]
    [ProducesResponseType<CalendarDateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CalendarDateResponse>> SetDate(
        Guid agencyId,
        Guid id,
        DateOnly date,
        CalendarDateRequest request,
        CancellationToken cancellationToken)
    {
        var calendarDate = await calendarService.SetDateAsync(
            agencyId, id, date, request.ExceptionType!.Value, cancellationToken);

        return Ok(calendarDate.ToResponse());
    }

    [HttpDelete("{id:guid}/dates/{date}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveDate(
        Guid agencyId,
        Guid id,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        await calendarService.RemoveDateAsync(agencyId, id, date, cancellationToken);
        return NoContent();
    }
}
