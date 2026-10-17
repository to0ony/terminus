using Terminus.Model.Common.Enums;

namespace Terminus.Model.Common;

/// <summary>Sezona voznog reda: razdoblje, tipovi dana (kalendari) i pravilo za praznike.</summary>
public interface ITimetableSeasonModel
{
    Guid Id { get; set; }
    string Name { get; set; }
    DateOnly StartDate { get; set; }
    DateOnly EndDate { get; set; }

    /// <summary>ISO kod države za praznike (npr. "HR"); null bez automatskih praznika.</summary>
    string? HolidayCountry { get; set; }

    SeasonDayMode HolidayMode { get; set; }

    /// <summary>Tip dana koji vozi na praznike kad je <see cref="HolidayMode"/> RunsAs.</summary>
    Guid? HolidayCalendarId { get; set; }

    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}

/// <summary>Dan sezone koji odstupa od dana u tjednu.</summary>
/// <param name="CalendarId">Tip dana kad je <paramref name="Mode"/> RunsAs; inače null.</param>
/// <param name="Note">Napomena, npr. naziv praznika.</param>
public sealed record SeasonDay(DateOnly Date, SeasonDayMode Mode, Guid? CalendarId, SeasonDaySource Source, string? Note);

/// <summary>Praznik iz popisa praznika države.</summary>
public sealed record PublicHoliday(DateOnly Date, string Name);

/// <summary>Sezona sa svojim tipovima dana i danima koji odstupaju.</summary>
/// <param name="DayTypes">Kalendari sezone, poredani po prvom danu u tjednu.</param>
/// <param name="Days">Dani koji odstupaju, poredani po datumu.</param>
public sealed record SeasonState(
    ITimetableSeasonModel Season,
    IReadOnlyList<IServiceCalendarModel> DayTypes,
    IReadOnlyList<SeasonDay> Days);
