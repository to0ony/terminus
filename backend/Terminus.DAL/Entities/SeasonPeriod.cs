namespace Terminus.DAL.Entities;

/// <summary>
/// Razdoblje sezone (npr. školski raspust) u kojem jedan tip dana vozi kao
/// drugi ili ne vozi. Iz njega nastaju dani sezone (<see cref="SeasonDay"/>)
/// s izvorom "razdoblje".
/// </summary>
public class SeasonPeriod
{
    public Guid Id { get; set; }
    public Guid SeasonId { get; set; }
    public string Name { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>Tip dana koji se u razdoblju zamjenjuje (npr. "Radni dan – nastava").</summary>
    public Guid ReplacedCalendarId { get; set; }

    /// <summary>SeasonDayMode: vozi kao tip dana ili ne vozi.</summary>
    public short Mode { get; set; }

    /// <summary>Tip dana koji vozi umjesto zamijenjenog kad je Mode "vozi kao".</summary>
    public Guid? CalendarId { get; set; }

    public TimetableSeason Season { get; set; } = null!;
}
