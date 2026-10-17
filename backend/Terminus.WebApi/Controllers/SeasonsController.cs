using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Terminus.Service.Common;
using Terminus.WebApi.Contracts.Seasons;
using Terminus.WebApi.Infrastructure;

namespace Terminus.WebApi.Controllers;

/// <summary>
/// Sezone voznog reda: razdoblje, tipovi dana (radni dan, subota, nedjelja…),
/// praznici i ručne iznimke. GTFS kalendari sezone i njihove iznimke izračunavaju
/// se iz toga, pa se ne uređuju preko /api/calendars.
/// </summary>
[ApiController]
[Route("api/seasons")]
[Produces("application/json")]
public class SeasonsController(ISeasonService seasonService, IOutputCacheStore outputCache) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SeasonResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SeasonResponse>>> List(CancellationToken cancellationToken)
    {
        var seasons = await seasonService.ListAsync(cancellationToken);
        return Ok(seasons.Select(s => s.ToResponse()).ToList());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<SeasonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SeasonResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var season = await seasonService.GetAsync(id, cancellationToken);
        return Ok(season.ToResponse());
    }

    /// <summary>
    /// Nova sezona s tipovima dana. S državom (HolidayCountry) praznici dolaze
    /// sami iz OpenHolidays API-ja (422 ako nije dostupan). 409 ako se razdoblje
    /// preklapa s drugom sezonom.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<SeasonResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SeasonResponse>> Create(CreateSeasonRequest request, CancellationToken cancellationToken)
    {
        var season = await seasonService.CreateAsync(
            request.ToModel(), request.DayTypes!.ToModels(), request.HolidayDayTypeIndex, cancellationToken);
        await EvictAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = season.State.Season.Id }, season.ToResponse());
    }

    /// <summary>Naziv, razdoblje, država i pravilo za praznike; praznici se ponovno dohvaćaju.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<SeasonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SeasonResponse>> Update(Guid id, UpdateSeasonRequest request, CancellationToken cancellationToken)
    {
        var season = await seasonService.UpdateAsync(id, request.ToModel(), cancellationToken);
        await EvictAsync(cancellationToken);
        return Ok(season.ToResponse());
    }

    /// <summary>Briše sezonu i njezine tipove dana; 409 ako ih koriste vožnje.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await seasonService.DeleteAsync(id, cancellationToken);
        await EvictAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Zamjenjuje tipove dana: postojeći se prepoznaju po Id-u, novi nemaju Id, a
    /// nenavedeni se brišu (409 ako ih koriste vožnje). Svaki dan u tjednu smije
    /// biti u najviše jednom tipu dana.
    /// </summary>
    [HttpPut("{id:guid}/day-types")]
    [ProducesResponseType<SeasonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SeasonResponse>> SaveDayTypes(Guid id, DayTypesRequest request, CancellationToken cancellationToken)
    {
        var season = await seasonService.SaveDayTypesAsync(id, request.DayTypes!.ToModels(), cancellationToken);
        await EvictAsync(cancellationToken);
        return Ok(season.ToResponse());
    }

    /// <summary>Ručna iznimka za dan (yyyy-MM-dd): vozi kao tip dana, ne vozi ili normalno.</summary>
    [HttpPut("{id:guid}/days/{date}")]
    [ProducesResponseType<SeasonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SeasonResponse>> SetDay(Guid id, DateOnly date, SeasonDayRequest request, CancellationToken cancellationToken)
    {
        var season = await seasonService.SetDayAsync(id, date, request.Mode, request.CalendarId, request.Note, cancellationToken);
        await EvictAsync(cancellationToken);
        return Ok(season.ToResponse());
    }

    /// <summary>Uklanja ručnu iznimku; za praznik ponovno vrijedi pravilo za praznike.</summary>
    [HttpDelete("{id:guid}/days/{date}")]
    [ProducesResponseType<SeasonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SeasonResponse>> ResetDay(Guid id, DateOnly date, CancellationToken cancellationToken)
    {
        var season = await seasonService.ResetDayAsync(id, date, cancellationToken);
        await EvictAsync(cancellationToken);
        return Ok(season.ToResponse());
    }

    /// <summary>Države za koje postoji popis praznika i prijedlog prema zoni prijevoznika.</summary>
    [HttpGet("holiday-countries")]
    [ProducesResponseType<HolidayCountriesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<HolidayCountriesResponse>> GetHolidayCountries(CancellationToken cancellationToken)
    {
        var countries = await seasonService.GetHolidayCountriesAsync(cancellationToken);
        return Ok(countries.ToResponse());
    }

    /// <summary>Zasloni i javni feed odmah vide promjenu kalendara.</summary>
    private ValueTask EvictAsync(CancellationToken cancellationToken) =>
        outputCache.EvictByTagAsync(PublicApi.TimetableTag, cancellationToken);
}
