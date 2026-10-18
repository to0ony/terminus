using System.Globalization;
using Terminus.Common.Exceptions;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Repository.Common;
using Terminus.Service.Common;

namespace Terminus.Service;

public class SeasonService(
    ISeasonRepository seasonRepository,
    IAgencyRepository agencyRepository,
    IPublicHolidaySource holidaySource) : ISeasonService
{
    /// <summary>Najdulja sezona; dulje razdoblje je gotovo sigurno greška u datumu.</summary>
    public const int MaxDays = 731;

    public const int MaxDayTypes = 10;

    public const int MaxNoteLength = 200;

    /// <summary>Po DayOfWeek (nedjelja je 0).</summary>
    private static readonly string[] DayNames =
        ["Nedjelja", "Ponedjeljak", "Utorak", "Srijeda", "Četvrtak", "Petak", "Subota"];

    /// <summary>Zona prijevoznika → država, za prijedlog države praznika.</summary>
    private static readonly Dictionary<string, string> CountryByTimezone = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Europe/Zagreb"] = "HR", ["Europe/Ljubljana"] = "SI", ["Europe/Vienna"] = "AT",
        ["Europe/Berlin"] = "DE", ["Europe/Belgrade"] = "RS", ["Europe/Budapest"] = "HU",
        ["Europe/Rome"] = "IT", ["Europe/Prague"] = "CZ", ["Europe/Bratislava"] = "SK",
        ["Europe/Warsaw"] = "PL", ["Europe/Madrid"] = "ES", ["Europe/Paris"] = "FR",
        ["Europe/Brussels"] = "BE", ["Europe/Amsterdam"] = "NL", ["Europe/Luxembourg"] = "LU",
        ["Europe/Zurich"] = "CH", ["Europe/Vaduz"] = "LI", ["Europe/Monaco"] = "MC",
        ["Europe/Andorra"] = "AD", ["Europe/Tirane"] = "AL", ["Europe/Sofia"] = "BG",
        ["Europe/Minsk"] = "BY", ["Europe/Tallinn"] = "EE", ["Europe/Dublin"] = "IE",
        ["Europe/Vilnius"] = "LT", ["Europe/Riga"] = "LV", ["Europe/Chisinau"] = "MD",
        ["Europe/Malta"] = "MT", ["Europe/Lisbon"] = "PT", ["Europe/Bucharest"] = "RO",
        ["Europe/Stockholm"] = "SE", ["Europe/San_Marino"] = "SM", ["Europe/Vatican"] = "VA",
        ["Africa/Johannesburg"] = "ZA", ["America/Mexico_City"] = "MX", ["America/Sao_Paulo"] = "BR"
    };

    public async Task<IReadOnlyList<SeasonDetail>> ListAsync(CancellationToken cancellationToken = default)
    {
        var seasons = await seasonRepository.ListAsync(cancellationToken);
        var counts = await seasonRepository.CountTripsAsync(
            seasons.SelectMany(s => s.DayTypes).Select(d => d.Id).ToList(), cancellationToken);

        return seasons.Select(s => new SeasonDetail(s, counts)).ToList();
    }

    public async Task<SeasonDetail> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var state = await LoadAsync(id, cancellationToken);
        return await DetailAsync(state, cancellationToken);
    }

    public async Task<SeasonDetail> CreateAsync(
        ITimetableSeasonModel season,
        IReadOnlyList<IServiceCalendarModel> dayTypes,
        int? holidayDayTypeIndex,
        CancellationToken cancellationToken = default)
    {
        season.Id = Guid.NewGuid();
        foreach (var dayType in dayTypes)
        {
            dayType.Id = Guid.NewGuid();
            dayType.Code = NewCode(season.StartDate);
        }

        var errors = new ValidationErrors();
        if (season.HolidayMode == SeasonDayMode.RunsAs)
        {
            if (holidayDayTypeIndex is { } index && index >= 0 && index < dayTypes.Count)
            {
                season.HolidayCalendarId = dayTypes[index].Id;
            }
            else
            {
                errors.Add("HolidayDayTypeIndex", "Odaberi tip dana koji vozi na praznike.");
            }
        }
        else
        {
            season.HolidayCalendarId = null;
        }
        Normalize(season, dayTypes);
        Validate(season, dayTypes, errors);
        errors.ThrowIfAny();

        await EnsureNoOverlapAsync(season, cancellationToken);

        var holidays = await FetchHolidaysAsync(season, cancellationToken);
        var state = new SeasonState(season, dayTypes, SeasonCalendarBuilder.ApplyHolidays(season, [], holidays));

        return await SaveAsync(state, cancellationToken);
    }

    public async Task<SeasonDetail> CopyAsync(Guid id, ITimetableSeasonModel season, bool withTrips, CancellationToken cancellationToken = default)
    {
        var source = await LoadAsync(id, cancellationToken);

        season.Id = Guid.NewGuid();
        season.HolidayCountry = source.Season.HolidayCountry;
        season.HolidayMode = source.Season.HolidayMode;

        // Stari tip dana → novi, za praznike i vožnje.
        var calendars = new Dictionary<Guid, Guid>();
        var dayTypes = source.DayTypes.Select(d =>
        {
            var copy = new ServiceCalendarModel
            {
                Id = Guid.NewGuid(),
                Code = NewCode(season.StartDate),
                Name = d.Name,
                Monday = d.Monday,
                Tuesday = d.Tuesday,
                Wednesday = d.Wednesday,
                Thursday = d.Thursday,
                Friday = d.Friday,
                Saturday = d.Saturday,
                Sunday = d.Sunday
            };
            calendars[d.Id] = copy.Id;
            return (IServiceCalendarModel)copy;
        }).ToList();

        season.HolidayCalendarId = source.Season.HolidayCalendarId is { } holiday && calendars.TryGetValue(holiday, out var mapped)
            ? mapped
            : null;

        Normalize(season, dayTypes);
        var errors = new ValidationErrors();
        Validate(season, dayTypes, errors);
        errors.ThrowIfAny();

        await EnsureNoOverlapAsync(season, cancellationToken);

        var holidays = await FetchHolidaysAsync(season, cancellationToken);
        var state = new SeasonState(season, dayTypes, SeasonCalendarBuilder.ApplyHolidays(season, [], holidays));

        return await SaveAsync(state, cancellationToken, withTrips ? calendars : null);
    }

    public async Task<SeasonDetail> UpdateAsync(Guid id, ITimetableSeasonModel season, CancellationToken cancellationToken = default)
    {
        var current = await LoadAsync(id, cancellationToken);
        season.Id = id;
        season.CreatedAt = current.Season.CreatedAt;
        if (season.HolidayMode != SeasonDayMode.RunsAs)
        {
            season.HolidayCalendarId = null;
        }

        var errors = new ValidationErrors();
        Normalize(season, current.DayTypes);
        Validate(season, current.DayTypes, errors);
        errors.ThrowIfAny();

        await EnsureNoOverlapAsync(season, cancellationToken);

        // Tipovi dana uvijek vrijede za cijelu sezonu.
        foreach (var dayType in current.DayTypes)
        {
            dayType.StartDate = season.StartDate;
            dayType.EndDate = season.EndDate;
        }

        var holidays = await FetchHolidaysAsync(season, cancellationToken);
        var days = SeasonCalendarBuilder.ApplyHolidays(season, current.Days, holidays);

        return await SaveAsync(new SeasonState(season, current.DayTypes, days), cancellationToken);
    }

    public async Task<SeasonDetail> SaveDayTypesAsync(Guid id, IReadOnlyList<IServiceCalendarModel> dayTypes, CancellationToken cancellationToken = default)
    {
        var current = await LoadAsync(id, cancellationToken);
        var season = current.Season;
        var existing = current.DayTypes.ToDictionary(d => d.Id);

        var errors = new ValidationErrors();
        foreach (var dayType in dayTypes)
        {
            if (dayType.Id == Guid.Empty)
            {
                dayType.Id = Guid.NewGuid();
                dayType.Code = NewCode(season.StartDate);
            }
            else if (existing.TryGetValue(dayType.Id, out var known))
            {
                dayType.Code = known.Code;
            }
            else
            {
                errors.Add("DayTypes", $"Tip dana {dayType.Id} ne pripada ovoj sezoni.");
            }
        }
        Normalize(season, dayTypes);
        Validate(season, dayTypes, errors);

        var kept = dayTypes.Select(d => d.Id).ToHashSet();
        var removed = current.DayTypes.Where(d => !kept.Contains(d.Id)).ToList();
        if (season.HolidayCalendarId is { } holidayCalendar && !kept.Contains(holidayCalendar))
        {
            errors.Add("DayTypes", "Tip dana koji vozi na praznike ne može se obrisati; prvo promijeni pravilo za praznike.");
        }
        errors.ThrowIfAny();

        if (removed.Count > 0)
        {
            var counts = await seasonRepository.CountTripsAsync(removed.Select(d => d.Id).ToList(), cancellationToken);
            var used = removed.Where(d => counts.ContainsKey(d.Id)).Select(d => $"{d.Name} ({counts[d.Id]} vožnji)").ToList();
            if (used.Count > 0)
            {
                throw new ConflictException($"Tipove dana koriste vožnje: {string.Join(", ", used)}.");
            }
        }

        // Ručni dani koji su vozili kao obrisani tip dana gube smisao.
        var days = current.Days
            .Where(d => d.Mode != SeasonDayMode.RunsAs || d.CalendarId is { } calendar && kept.Contains(calendar))
            .ToList();

        return await SaveAsync(new SeasonState(season, dayTypes, days), cancellationToken);
    }

    public async Task<SeasonDetail> SetDayAsync(
        Guid id,
        DateOnly date,
        SeasonDayMode mode,
        Guid? calendarId,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var current = await LoadAsync(id, cancellationToken);
        var season = current.Season;
        note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        var errors = new ValidationErrors();
        if (date < season.StartDate || date > season.EndDate)
        {
            errors.Add("Date", "Datum nije unutar sezone.");
        }
        if (!Enum.IsDefined(mode))
        {
            errors.Add("Mode", "Nepoznat način vožnje.");
        }
        else if (mode == SeasonDayMode.RunsAs && !current.DayTypes.Any(d => d.Id == calendarId))
        {
            errors.Add("CalendarId", "Odaberi tip dana ove sezone.");
        }
        if (note?.Length > MaxNoteLength)
        {
            errors.Add("Note", $"Napomena smije imati najviše {MaxNoteLength} znakova.");
        }
        errors.ThrowIfAny();

        var day = new SeasonDay(date, mode, mode == SeasonDayMode.RunsAs ? calendarId : null, SeasonDaySource.Manual, note);
        var days = current.Days.Where(d => d.Date != date).Append(day).OrderBy(d => d.Date).ToList();

        return await SaveAsync(current with { Days = days }, cancellationToken);
    }

    public async Task<SeasonDetail> ResetDayAsync(Guid id, DateOnly date, CancellationToken cancellationToken = default)
    {
        var current = await LoadAsync(id, cancellationToken);
        if (!current.Days.Any(d => d.Date == date && d.Source == SeasonDaySource.Manual))
        {
            throw new NotFoundException($"Za {date:yyyy-MM-dd} nema ručne iznimke.");
        }

        var days = current.Days.Where(d => d.Date != date).ToList();

        // Ako je taj dan praznik, ponovno vrijedi pravilo za praznike.
        var holidays = await FetchHolidaysAsync(current.Season, cancellationToken, fallback: current.Days);
        days = [.. SeasonCalendarBuilder.ApplyHolidays(current.Season, days, holidays)];

        return await SaveAsync(current with { Days = days }, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var current = await LoadAsync(id, cancellationToken);
        var counts = await seasonRepository.CountTripsAsync(current.DayTypes.Select(d => d.Id).ToList(), cancellationToken);
        if (counts.Count > 0)
        {
            throw new ConflictException($"Tipove dana sezone koristi {counts.Values.Sum()} vožnji, pa se sezona ne može obrisati.");
        }

        await seasonRepository.DeleteAsync(id, cancellationToken);
    }

    public async Task<HolidayCountries> GetHolidayCountriesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<HolidayCountry> countries;
        try
        {
            countries = await holidaySource.GetCountriesAsync(cancellationToken);
        }
        catch (HolidaySourceException ex)
        {
            throw HolidaysUnavailable(ex);
        }

        var agency = await agencyRepository.GetFirstAsync(cancellationToken);
        var suggested = agency is not null
            && CountryByTimezone.TryGetValue(agency.Timezone, out var code)
            && countries.Any(c => c.Code == code)
                ? code
                : null;

        return new HolidayCountries(countries, suggested);
    }

    private async Task<SeasonState> LoadAsync(Guid id, CancellationToken cancellationToken) =>
        await seasonRepository.GetAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Sezona {id} ne postoji.");

    private async Task<SeasonDetail> SaveAsync(
        SeasonState state,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<Guid, Guid>? copyTrips = null)
    {
        var calendarDates = SeasonCalendarBuilder.CalendarDates(state.Season, state.DayTypes, state.Days);
        await seasonRepository.SaveAsync(state, calendarDates, copyTrips, cancellationToken);

        // Natrag iz baze, s vremenima izmjene koja postavlja baza.
        return await GetAsync(state.Season.Id, cancellationToken);
    }

    private async Task<SeasonDetail> DetailAsync(SeasonState state, CancellationToken cancellationToken) =>
        new(state, await seasonRepository.CountTripsAsync(state.DayTypes.Select(d => d.Id).ToList(), cancellationToken));

    /// <param name="fallback">
    /// Ako izvor nije dostupan, vrijede praznici koji su već zapisani u danima
    /// sezone; bez toga je nedostupan izvor greška.
    /// </param>
    private async Task<IReadOnlyList<PublicHoliday>> FetchHolidaysAsync(
        ITimetableSeasonModel season,
        CancellationToken cancellationToken,
        IReadOnlyList<SeasonDay>? fallback = null)
    {
        if (season.HolidayCountry is null)
        {
            return [];
        }

        try
        {
            return await holidaySource.GetHolidaysAsync(season.HolidayCountry, season.StartDate, season.EndDate, cancellationToken);
        }
        catch (HolidaySourceException ex)
        {
            if (fallback is null)
            {
                throw HolidaysUnavailable(ex);
            }
            return fallback
                .Where(d => d.Source == SeasonDaySource.Holiday)
                .Select(d => new PublicHoliday(d.Date, d.Note ?? string.Empty))
                .ToList();
        }
    }

    private async Task EnsureNoOverlapAsync(ITimetableSeasonModel season, CancellationToken cancellationToken)
    {
        var other = await seasonRepository.FindOverlappingAsync(season.StartDate, season.EndDate, season.Id, cancellationToken);
        if (other is not null)
        {
            throw new ConflictException(
                $"Razdoblje se preklapa sa sezonom „{other.Name}” ({Format(other.StartDate)} – {Format(other.EndDate)}).");
        }
    }

    private static void Normalize(ITimetableSeasonModel season, IReadOnlyList<IServiceCalendarModel> dayTypes)
    {
        season.Name = season.Name?.Trim() ?? string.Empty;
        season.HolidayCountry = string.IsNullOrWhiteSpace(season.HolidayCountry)
            ? null
            : season.HolidayCountry.Trim().ToUpperInvariant();

        foreach (var dayType in dayTypes)
        {
            dayType.Name = dayType.Name?.Trim() ?? string.Empty;
            dayType.StartDate = season.StartDate;
            dayType.EndDate = season.EndDate;
        }
    }

    private static void Validate(ITimetableSeasonModel season, IReadOnlyList<IServiceCalendarModel> dayTypes, ValidationErrors errors)
    {
        if (season.Name.Length == 0)
        {
            errors.Add("Name", "Naziv je obavezan.");
        }
        if (season.EndDate < season.StartDate)
        {
            errors.Add("EndDate", "Kraj sezone ne smije biti prije početka.");
        }
        else if (season.EndDate.DayNumber - season.StartDate.DayNumber + 1 > MaxDays)
        {
            errors.Add("EndDate", $"Sezona može trajati najviše {MaxDays} dana.");
        }
        if (season.HolidayCountry is { Length: not 2 })
        {
            errors.Add("HolidayCountry", "Država se zadaje dvoslovnim kodom (npr. HR).");
        }
        if (!Enum.IsDefined(season.HolidayMode))
        {
            errors.Add("HolidayMode", "Nepoznato pravilo za praznike.");
        }
        else if (season.HolidayMode == SeasonDayMode.RunsAs
            && season.HolidayCalendarId is { } holidayCalendar
            && !dayTypes.Any(d => d.Id == holidayCalendar))
        {
            errors.Add("HolidayCalendarId", "Tip dana za praznike mora pripadati sezoni.");
        }
        else if (season.HolidayMode == SeasonDayMode.RunsAs && season.HolidayCalendarId is null && !errors.Contains("HolidayDayTypeIndex"))
        {
            errors.Add("HolidayCalendarId", "Odaberi tip dana koji vozi na praznike.");
        }

        if (dayTypes.Count == 0)
        {
            errors.Add("DayTypes", "Sezona mora imati barem jedan tip dana.");
        }
        else if (dayTypes.Count > MaxDayTypes)
        {
            errors.Add("DayTypes", $"Sezona može imati najviše {MaxDayTypes} tipova dana.");
        }
        if (dayTypes.Any(d => d.Name.Length == 0))
        {
            errors.Add("DayTypes", "Svaki tip dana mora imati naziv.");
        }
        if (dayTypes.GroupBy(d => d.Name, StringComparer.CurrentCultureIgnoreCase).Any(g => g.Count() > 1))
        {
            errors.Add("DayTypes", "Nazivi tipova dana moraju biti različiti.");
        }

        // Svaki dan u tjednu vozi najviše jedan tip dana.
        foreach (var weekday in Enum.GetValues<DayOfWeek>())
        {
            var owners = dayTypes.Where(d => ServiceCalendarRules.RunsOnWeekday(d, weekday)).Select(d => d.Name).ToList();
            if (owners.Count > 1)
            {
                errors.Add("DayTypes", $"{DayNames[(int)weekday]} je u više tipova dana ({string.Join(", ", owners)}).");
            }
        }
    }

    /// <summary>GTFS service_id: početak sezone i nasumični nastavak, jedinstven u praksi.</summary>
    private static string NewCode(DateOnly start) =>
        $"{start.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}-{Guid.NewGuid().ToString("N")[..6]}";

    private static string Format(DateOnly date) => date.ToString("d. M. yyyy.", CultureInfo.InvariantCulture);

    private static UnprocessableException HolidaysUnavailable(HolidaySourceException ex) => new(
        "Praznici se trenutno ne mogu dohvatiti.",
        [$"{ex.Message} Pokušaj ponovno ili odaberi „bez praznika” pa ih dodaj ručno."]);
}
