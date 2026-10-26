using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Terminus.Common.Exceptions;
using Terminus.Service.Common;
using Terminus.WebApi.Contracts.Routes;
using Terminus.WebApi.Contracts.Timetables;
using Terminus.WebApi.Infrastructure;

namespace Terminus.WebApi.Controllers;

/// <summary>
/// Javna karta uživo (/karta), bez prijave: aktivne stanice, polasci i vožnje
/// dana za simulirana vozila. Dostupna samo ako je u postavkama uključen
/// PublicMapEnabled; inače 404.
/// </summary>
[ApiController]
[Route("api/public/map")]
[AllowAnonymous]
[EnableRateLimiting(PublicApi.RateLimitPolicy)]
[OutputCache(PolicyName = PublicApi.MapCachePolicy)]
[Produces("application/json")]
public class PublicMapController(
    ISettingsService settingsService,
    IStopService stopService,
    ITimetableService timetableService,
    IDepartureService departureService,
    ILiveMapService liveMapService,
    TimeProvider timeProvider) : ControllerBase
{
    /// <summary>Aktivne stanice s koordinatama unutar područja karte, najviše 2000.</summary>
    [HttpGet("stops/area")]
    [ProducesResponseType<StopsInAreaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StopsInAreaResponse>> FindStopsInArea(
        [FromQuery] StopAreaQuery query,
        CancellationToken cancellationToken)
    {
        await EnsureEnabledAsync(cancellationToken);
        var result = await stopService.FindInAreaAsync(query.ToBounds(), true, cancellationToken);
        return Ok(result.ToResponse());
    }

    /// <summary>Područje svih stanica, za početni prikaz; 204 ako nijedna nema koordinate.</summary>
    [HttpGet("stops/extent")]
    [ProducesResponseType<GeoBoundsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GeoBoundsResponse>> GetStopsExtent(CancellationToken cancellationToken)
    {
        await EnsureEnabledAsync(cancellationToken);
        var extent = await stopService.GetExtentAsync(cancellationToken);
        return extent is null ? NoContent() : Ok(extent.ToResponse());
    }

    /// <summary>Linije koje staju na stanici (za vrstu prijevoza uz polaske).</summary>
    [HttpGet("stops/{id:guid}/routes")]
    [ProducesResponseType<IReadOnlyList<RouteResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<RouteResponse>>> GetStopRoutes(Guid id, CancellationToken cancellationToken)
    {
        await EnsureEnabledAsync(cancellationToken);
        var routes = await timetableService.GetStopRoutesAsync(id, cancellationToken);
        return Ok(routes.Select(r => r.ToResponse()).ToList());
    }

    /// <summary>Svi polasci sa stanice jednog dana (bez datuma današnji).</summary>
    [HttpGet("stops/{id:guid}/departures")]
    [ProducesResponseType<StopDayDeparturesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StopDayDeparturesResponse>> GetStopDepartures(
        Guid id,
        [FromQuery] StopDayDeparturesQuery query,
        CancellationToken cancellationToken)
    {
        await EnsureEnabledAsync(cancellationToken);
        var result = await departureService.GetForDayAsync(id, query.Date, timeProvider.GetUtcNow(), cancellationToken);
        return Ok(result.ToResponse());
    }

    /// <summary>Vožnje linije jednog dana s vremenima, trasama i stanicama.</summary>
    [HttpGet("routes/{id:guid}/live")]
    [ProducesResponseType<LiveRouteDayResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LiveRouteDayResponse>> GetRouteLive(
        Guid id,
        [FromQuery] LiveMapQuery query,
        CancellationToken cancellationToken)
    {
        await EnsureEnabledAsync(cancellationToken);
        var day = await liveMapService.GetRouteDayAsync(id, query.Date, timeProvider.GetUtcNow(), cancellationToken);
        return Ok(day.ToResponse());
    }

    /// <summary>Vožnje svih linija jednog dana (sva vozila).</summary>
    [HttpGet("live")]
    [ProducesResponseType<LiveNetworkDayResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LiveNetworkDayResponse>> GetNetworkLive(
        [FromQuery] LiveMapQuery query,
        CancellationToken cancellationToken)
    {
        await EnsureEnabledAsync(cancellationToken);
        var day = await liveMapService.GetNetworkDayAsync(query.Date, timeProvider.GetUtcNow(), cancellationToken);
        return Ok(day.ToResponse());
    }

    private async Task EnsureEnabledAsync(CancellationToken cancellationToken)
    {
        var settings = await settingsService.GetAsync(cancellationToken);
        if (!settings.PublicMapEnabled)
        {
            throw new NotFoundException("Karta nije javno objavljena.");
        }
    }
}
