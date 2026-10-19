using System.ComponentModel.DataAnnotations;
using Terminus.Model.Common.Enums;

namespace Terminus.WebApi.Contracts.Seasons;

/// <param name="HolidayCountry">ISO kod države za praznike; null bez automatskih praznika.</param>
/// <param name="HolidayCalendarId">Tip dana koji vozi na praznike kad je HolidayMode RunsAs.</param>
/// <param name="DayTypes">Tipovi dana (GTFS kalendari sezone), od ponedjeljka.</param>
/// <param name="Days">Dani koji odstupaju od dana u tjednu: praznici, ručne iznimke i dani razdoblja.</param>
/// <param name="Periods">Razdoblja (npr. školski raspusti), po početku.</param>
public record SeasonResponse(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string? HolidayCountry,
    SeasonDayMode HolidayMode,
    Guid? HolidayCalendarId,
    IReadOnlyList<DayTypeResponse> DayTypes,
    IReadOnlyList<SeasonDayResponse> Days,
    IReadOnlyList<SeasonPeriodResponse> Periods,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <param name="ReplacedCalendarId">Tip dana koji se u razdoblju zamjenjuje.</param>
/// <param name="Mode">RunsAs (vozi kao CalendarId) ili NoService.</param>
public record SeasonPeriodResponse(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid ReplacedCalendarId,
    SeasonDayMode Mode,
    Guid? CalendarId);

/// <param name="Code">GTFS service_id.</param>
/// <param name="TripCount">Broj vožnji koje voze po ovom tipu dana.</param>
public record DayTypeResponse(
    Guid Id,
    string Code,
    string Name,
    bool Monday,
    bool Tuesday,
    bool Wednesday,
    bool Thursday,
    bool Friday,
    bool Saturday,
    bool Sunday,
    int TripCount);

/// <param name="CalendarId">Tip dana kad je Mode RunsAs.</param>
/// <param name="Note">Napomena, kod praznika njegov naziv.</param>
public record SeasonDayResponse(
    DateOnly Date,
    SeasonDayMode Mode,
    Guid? CalendarId,
    SeasonDaySource Source,
    string? Note);

/// <summary>Nova sezona s tipovima dana.</summary>
public class CreateSeasonRequest
{
    [Required]
    public string? Name { get; set; }

    [Required]
    public DateOnly? StartDate { get; set; }

    [Required]
    public DateOnly? EndDate { get; set; }

    [Required]
    public List<DayTypeRequest>? DayTypes { get; set; }

    /// <summary>ISO kod države (npr. "HR"); bez njega nema automatskih praznika.</summary>
    public string? HolidayCountry { get; set; }

    public SeasonDayMode HolidayMode { get; set; }

    /// <summary>Indeks tipa dana u DayTypes koji vozi na praznike (za HolidayMode RunsAs).</summary>
    public int? HolidayDayTypeIndex { get; set; }
}

/// <summary>Naziv, razdoblje i pravilo za praznike; tipovi dana se mijenjaju zasebno.</summary>
public class UpdateSeasonRequest
{
    [Required]
    public string? Name { get; set; }

    [Required]
    public DateOnly? StartDate { get; set; }

    [Required]
    public DateOnly? EndDate { get; set; }

    public string? HolidayCountry { get; set; }

    public SeasonDayMode HolidayMode { get; set; }

    public Guid? HolidayCalendarId { get; set; }
}

/// <summary>Kopija sezone za drugo razdoblje.</summary>
public class CopySeasonRequest
{
    [Required]
    public string? Name { get; set; }

    [Required]
    public DateOnly? StartDate { get; set; }

    [Required]
    public DateOnly? EndDate { get; set; }

    /// <summary>Kopiraju se i sve vožnje sezone u odgovarajuće tipove dana.</summary>
    public bool WithTrips { get; set; } = true;
}

public class SeasonPeriodsRequest
{
    [Required]
    public List<SeasonPeriodRequest>? Periods { get; set; }
}

/// <summary>Razdoblje sezone; bez Id-a je novo.</summary>
public class SeasonPeriodRequest
{
    public Guid? Id { get; set; }

    [Required]
    public string? Name { get; set; }

    [Required]
    public DateOnly? StartDate { get; set; }

    [Required]
    public DateOnly? EndDate { get; set; }

    /// <summary>Tip dana koji se u razdoblju zamjenjuje (npr. radni dan za vrijeme nastave).</summary>
    [Required]
    public Guid? ReplacedCalendarId { get; set; }

    /// <summary>RunsAs (vozi kao CalendarId) ili NoService.</summary>
    public SeasonDayMode Mode { get; set; } = SeasonDayMode.RunsAs;

    public Guid? CalendarId { get; set; }
}

public class DayTypesRequest
{
    [Required]
    public List<DayTypeRequest>? DayTypes { get; set; }
}

/// <summary>Tip dana; bez Id-a je novi.</summary>
public class DayTypeRequest
{
    public Guid? Id { get; set; }

    [Required]
    public string? Name { get; set; }

    public bool Monday { get; set; }
    public bool Tuesday { get; set; }
    public bool Wednesday { get; set; }
    public bool Thursday { get; set; }
    public bool Friday { get; set; }
    public bool Saturday { get; set; }
    public bool Sunday { get; set; }
}

/// <summary>Ručna iznimka za dan sezone.</summary>
public class SeasonDayRequest
{
    public SeasonDayMode Mode { get; set; }

    /// <summary>Tip dana za Mode RunsAs.</summary>
    public Guid? CalendarId { get; set; }

    public string? Note { get; set; }
}

/// <param name="Suggested">Država prema zoni prijevoznika, ako postoji na popisu.</param>
public record HolidayCountriesResponse(IReadOnlyList<HolidayCountryResponse> Countries, string? Suggested);

public record HolidayCountryResponse(string Code, string Name);
