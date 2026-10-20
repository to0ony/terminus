using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Service.Common;

/// <summary>
/// Sezone voznog reda: razdoblje, tipovi dana (npr. radni dan, subota, nedjelja)
/// i dani koji odstupaju (praznici, ručne iznimke). GTFS kalendari i njihove
/// iznimke izračunavaju se iz toga pri svakoj promjeni.
/// </summary>
public interface ISeasonService
{
    Task<IReadOnlyList<SeasonDetail>> ListAsync(CancellationToken cancellationToken = default);

    Task<SeasonDetail> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Nova sezona s tipovima dana; praznici države dolaze sami.</summary>
    /// <param name="holidayDayTypeIndex">Tip dana (indeks u <paramref name="dayTypes"/>) koji vozi na praznike kad je pravilo RunsAs.</param>
    Task<SeasonDetail> CreateAsync(
        ITimetableSeasonModel season,
        IReadOnlyList<IServiceCalendarModel> dayTypes,
        int? holidayDayTypeIndex,
        CancellationToken cancellationToken = default);

    /// <summary>Naziv, razdoblje, država i pravilo za praznike; praznici se ponovno dohvaćaju.</summary>
    Task<SeasonDetail> UpdateAsync(Guid id, ITimetableSeasonModel season, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zamjenjuje tipove dana: postojeći se prepoznaju po Id-u, novi nemaju Id,
    /// a nenavedeni se brišu (ako ih ne koristi nijedna vožnja).
    /// </summary>
    Task<SeasonDetail> SaveDayTypesAsync(Guid id, IReadOnlyList<IServiceCalendarModel> dayTypes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zamjenjuje razdoblja sezone (npr. školske raspuste): u razdoblju jedan tip dana
    /// vozi kao drugi ili ne vozi. Novo razdoblje nema Id. Ručne iznimke i praznici
    /// imaju prednost pred razdobljem.
    /// </summary>
    Task<SeasonDetail> SavePeriodsAsync(Guid id, IReadOnlyList<SeasonPeriod> periods, CancellationToken cancellationToken = default);

    /// <summary>Ručna iznimka za dan: vozi kao tip dana, ne vozi ili normalno (npr. za praznik).</summary>
    Task<SeasonDetail> SetDayAsync(
        Guid id,
        DateOnly date,
        SeasonDayMode mode,
        Guid? calendarId,
        string? note,
        CancellationToken cancellationToken = default);

    /// <summary>Uklanja ručnu iznimku; za praznik ponovno vrijedi pravilo za praznike.</summary>
    Task<SeasonDetail> ResetDayAsync(Guid id, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Nova sezona za drugo razdoblje s istim tipovima dana i pravilom za praznike
    /// (praznici se dohvaćaju za novo razdoblje, ručne iznimke se ne kopiraju jer su
    /// vezane uz datume). Uz <paramref name="withTrips"/> kopiraju se i sve vožnje.
    /// </summary>
    /// <param name="season">Naziv i razdoblje nove sezone.</param>
    Task<SeasonDetail> CopyAsync(Guid id, ITimetableSeasonModel season, bool withTrips, CancellationToken cancellationToken = default);

    /// <summary>
    /// Briše sezonu s tipovima dana; bez <paramref name="withTrips"/> ne smije ih
    /// koristiti nijedna vožnja, a s njim se brišu i te vožnje.
    /// </summary>
    Task DeleteAsync(Guid id, bool withTrips = false, CancellationToken cancellationToken = default);

    /// <summary>Države s popisom praznika i prijedlog prema zoni prijevoznika.</summary>
    Task<HolidayCountries> GetHolidayCountriesAsync(CancellationToken cancellationToken = default);
}

/// <param name="TripCounts">Broj vožnji po tipu dana; tipovi bez vožnji nisu u rječniku.</param>
public sealed record SeasonDetail(SeasonState State, IReadOnlyDictionary<Guid, int> TripCounts);

/// <param name="Suggested">Država prema zoni prvog prijevoznika, ako je na popisu.</param>
public sealed record HolidayCountries(IReadOnlyList<HolidayCountry> Countries, string? Suggested);

/// <param name="Code">ISO 3166-1 alpha-2, npr. "HR".</param>
public sealed record HolidayCountry(string Code, string Name);

/// <summary>Popis državnih praznika (npr. OpenHolidays API).</summary>
public interface IPublicHolidaySource
{
    /// <exception cref="HolidaySourceException">Izvor nije dostupan.</exception>
    Task<IReadOnlyList<HolidayCountry>> GetCountriesAsync(CancellationToken cancellationToken = default);

    /// <returns>Praznici koji vrijede u cijeloj državi, poredani po datumu.</returns>
    /// <exception cref="HolidaySourceException">Izvor nije dostupan.</exception>
    Task<IReadOnlyList<PublicHoliday>> GetHolidaysAsync(string country, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}

public sealed class HolidaySourceException(string message, Exception? innerException = null)
    : Exception(message, innerException);
