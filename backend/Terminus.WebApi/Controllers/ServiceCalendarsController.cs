using Microsoft.AspNetCore.Mvc;
using Terminus.Common;
using Terminus.Service.Common;
using Terminus.WebApi.Contracts.Calendars;

namespace Terminus.WebApi.Controllers;

[ApiController]
[Route("api/calendars")]
[Produces("application/json")]
public class ServiceCalendarsController(
    IServiceCalendarService calendarService,
    ITimetableService timetableService,
    TimeProvider timeProvider) : ControllerBase
{
    /// <summary>
    /// Kalendari koji voze zadani dan (bez njega danas) i zadnji dan s voznim
    /// redom, npr. za upozorenje da feed uskoro istječe.
    /// </summary>
    [HttpGet("overview")]
    [ProducesResponseType<ServiceOverviewResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ServiceOverviewResponse>> GetOverview(
        [FromQuery] ServiceOverviewQuery query,
        CancellationToken cancellationToken)
    {
        var overview = await timetableService.GetServiceOverviewAsync(query.Date, timeProvider.GetUtcNow(), cancellationToken);
        return Ok(new ServiceOverviewResponse(
            overview.Date,
            overview.ActiveCalendars.Select(c => c.ToResponse()).ToList(),
            overview.LastServiceDate));
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<ServiceCalendarResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ServiceCalendarResponse>>> Find(
        [FromQuery] ServiceCalendarListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await calendarService.FindAsync(query.ToFilter(), cancellationToken);
        return Ok(result.Map(c => c.ToResponse()));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ServiceCalendarResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceCalendarResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var calendar = await calendarService.GetAsync(id, cancellationToken);
        return Ok(calendar.ToResponse());
    }

    [HttpPost]
    [ProducesResponseType<ServiceCalendarResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ServiceCalendarResponse>> Create(
        ServiceCalendarRequest request,
        CancellationToken cancellationToken)
    {
        var created = await calendarService.CreateAsync(request.ToModel(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<ServiceCalendarResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ServiceCalendarResponse>> Update(
        Guid id,
        ServiceCalendarRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await calendarService.UpdateAsync(id, request.ToModel(), cancellationToken);
        return Ok(updated.ToResponse());
    }

    /// <summary>Briše kalendar i njegove iznimke. Ne uspijeva (409) ako ga koriste vožnje.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await calendarService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Sve iznimke kalendara, poredane po datumu.</summary>
    [HttpGet("{id:guid}/dates")]
    [ProducesResponseType<IReadOnlyList<CalendarDateResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CalendarDateResponse>>> GetDates(
        Guid id,
        CancellationToken cancellationToken)
    {
        var dates = await calendarService.GetDatesAsync(id, cancellationToken);
        return Ok(dates.Select(d => d.ToResponse()).ToList());
    }

    /// <summary>Dodaje iznimku za datum (yyyy-MM-dd) ili mijenja postojeću.</summary>
    [HttpPut("{id:guid}/dates/{date}")]
    [ProducesResponseType<CalendarDateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CalendarDateResponse>> SetDate(
        Guid id,
        DateOnly date,
        CalendarDateRequest request,
        CancellationToken cancellationToken)
    {
        var calendarDate = await calendarService.SetDateAsync(
            id, date, request.ExceptionType!.Value, cancellationToken);

        return Ok(calendarDate.ToResponse());
    }

    [HttpDelete("{id:guid}/dates/{date}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveDate(
        Guid id,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        await calendarService.RemoveDateAsync(id, date, cancellationToken);
        return NoContent();
    }
}
