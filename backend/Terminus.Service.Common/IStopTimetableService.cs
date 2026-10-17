using Terminus.Model.Common;

namespace Terminus.Service.Common;

/// <summary>Vozni red stanice za ispis (PDF na stanici).</summary>
public interface IStopTimetableService
{
    /// <summary>
    /// Svi polasci sa stanice u razdoblju, po liniji i smjeru, a unutar toga po
    /// danima u tjednu s istim voznim redom (npr. radni dan, subota, nedjelja).
    /// Dani koji odstupaju postaju napomene. Bez <paramref name="from"/> razdoblje
    /// počinje danas u zoni prijevoznika, a bez <paramref name="to"/> traje do
    /// zadnjeg dana s voznim redom.
    /// </summary>
    Task<StopTimetable> GetAsync(Guid stopId, DateOnly? from, DateOnly? to, DateTimeOffset now, CancellationToken cancellationToken = default);
}

/// <param name="AgencyName">Naziv prvog unesenog prijevoznika.</param>
/// <param name="From">Prvi dan razdoblja.</param>
/// <param name="To">Zadnji dan razdoblja.</param>
public sealed record StopTimetable(
    IStopModel Stop,
    string AgencyName,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<TimetableSection> Sections);

/// <summary>Polasci jedne linije u jednom smjeru (i s jednog perona kod kolodvora).</summary>
/// <param name="Columns">Stupci voznog reda: prvo dani u tjednu, zatim posebni datumi.</param>
/// <param name="Footnotes">Objašnjenja slova uz pojedine polaske.</param>
/// <param name="RunsAs">Datumi na koje vozi kao neki drugi stupac (npr. praznik kao nedjelja).</param>
/// <param name="NoServiceDates">Datumi na koje inače vozi, a tada ne.</param>
public sealed record TimetableSection(
    string? RouteShortName,
    string? RouteLongName,
    string? RouteColor,
    string? RouteTextColor,
    string Headsign,
    string? Platform,
    IReadOnlyList<TimetableColumn> Columns,
    IReadOnlyList<TimetableFootnote> Footnotes,
    IReadOnlyList<TimetableRunsAs> RunsAs,
    IReadOnlyList<DateOnly> NoServiceDates);

/// <param name="Weekdays">Dani u tjednu stupca, od ponedjeljka; prazno za stupac posebnih datuma.</param>
/// <param name="SpecialDates">Datumi stupca bez dana u tjednu; inače prazno.</param>
/// <param name="Times">Polasci poredani po vremenu.</param>
public sealed record TimetableColumn(
    IReadOnlyList<DayOfWeek> Weekdays,
    IReadOnlyList<DateOnly> SpecialDates,
    IReadOnlyList<TimetableTime> Times);

/// <param name="Seconds">GTFS vrijeme od početka dana voznog reda (može biti iznad 24 h).</param>
/// <param name="Mark">Oznaka napomene ("a", "b", …) ili null ako polazak vozi na sve dane stupca.</param>
public sealed record TimetableTime(int Seconds, string? Mark);

/// <param name="RunsOnly">true: vozi samo na <paramref name="Dates"/>; false: ne vozi na njih.</param>
public sealed record TimetableFootnote(string Mark, bool RunsOnly, IReadOnlyList<DateOnly> Dates);

/// <param name="ColumnIndex">Stupac istog odjeljka čiji vozni red tada vrijedi.</param>
public sealed record TimetableRunsAs(int ColumnIndex, IReadOnlyList<DateOnly> Dates);
