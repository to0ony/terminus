namespace Terminus.DAL.Entities;

/// <summary>Dan sezone koji odstupa od dana u tjednu (praznik ili ručna iznimka).</summary>
public class SeasonDay
{
    public Guid SeasonId { get; set; }
    public DateOnly Date { get; set; }

    /// <summary>SeasonDayMode: normalno, vozi kao tip dana ili ne vozi.</summary>
    public short Mode { get; set; }

    /// <summary>Tip dana koji vozi kad je Mode "vozi kao".</summary>
    public Guid? CalendarId { get; set; }

    /// <summary>SeasonDaySource: ručno ili praznik.</summary>
    public short Source { get; set; }

    /// <summary>Napomena, npr. naziv praznika.</summary>
    public string? Note { get; set; }

    public TimetableSeason Season { get; set; } = null!;
}
