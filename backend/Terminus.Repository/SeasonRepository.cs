using Microsoft.EntityFrameworkCore;
using Npgsql;
using Terminus.Common.Exceptions;
using Terminus.DAL;
using Terminus.DAL.Entities;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Repository.Common;
using Terminus.Repository.Mapping;
using SeasonDay = Terminus.Model.Common.SeasonDay;

namespace Terminus.Repository;

public class SeasonRepository(TerminusDbContext context) : ISeasonRepository
{
    public async Task<IReadOnlyList<SeasonState>> ListAsync(CancellationToken cancellationToken = default)
    {
        var seasons = await context.TimetableSeasons
            .AsNoTracking()
            .Include(s => s.Calendars)
            .Include(s => s.Days)
            .OrderBy(s => s.StartDate)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return seasons.Select(ToState).ToList();
    }

    public async Task<SeasonState?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var season = await context.TimetableSeasons
            .AsNoTracking()
            .Include(s => s.Calendars)
            .Include(s => s.Days)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        return season is null ? null : ToState(season);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> CountTripsAsync(IReadOnlyCollection<Guid> calendarIds, CancellationToken cancellationToken = default) =>
        await context.Trips
            .Where(t => calendarIds.Contains(t.ServiceCalendarId))
            .GroupBy(t => t.ServiceCalendarId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

    public async Task<ITimetableSeasonModel?> FindOverlappingAsync(DateOnly start, DateOnly end, Guid? exceptId, CancellationToken cancellationToken = default)
    {
        var season = await context.TimetableSeasons
            .AsNoTracking()
            .Where(s => s.Id != exceptId && s.StartDate <= end && s.EndDate >= start)
            .OrderBy(s => s.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        return season is null ? null : ToModel(season);
    }

    public async Task SaveAsync(SeasonState state, IReadOnlyList<ICalendarDateModel> calendarDates, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var model = state.Season;

        var season = await context.TimetableSeasons
            .Include(s => s.Calendars)
            .FirstOrDefaultAsync(s => s.Id == model.Id, cancellationToken);

        if (season is null)
        {
            season = new TimetableSeason { Id = model.Id };
            context.TimetableSeasons.Add(season);
        }

        season.Name = model.Name;
        season.StartDate = model.StartDate;
        season.EndDate = model.EndDate;
        season.HolidayCountry = model.HolidayCountry;
        season.HolidayMode = (short)model.HolidayMode;
        // Sezona i kalendari se međusobno referenciraju, pa se tip dana za
        // praznike postavlja tek kad su kalendari upisani.
        season.HolidayCalendarId = null;

        var keep = state.DayTypes.Select(d => d.Id).ToHashSet();
        foreach (var removed in season.Calendars.Where(c => !keep.Contains(c.Id)).ToList())
        {
            context.ServiceCalendars.Remove(removed);
        }

        foreach (var dayType in state.DayTypes)
        {
            var calendar = season.Calendars.FirstOrDefault(c => c.Id == dayType.Id);
            if (calendar is null)
            {
                calendar = new ServiceCalendar { Id = dayType.Id, SeasonId = season.Id };
                context.ServiceCalendars.Add(calendar);
            }
            ServiceCalendarMapper.Apply(dayType, calendar);
        }

        await SaveChangesAsync(cancellationToken);

        season.HolidayCalendarId = model.HolidayCalendarId;

        // Dani i iznimke se uvijek pišu ispočetka; iznimke su izračunate iz dana.
        await context.SeasonDays.Where(d => d.SeasonId == season.Id).ExecuteDeleteAsync(cancellationToken);
        await context.CalendarDates.Where(d => keep.Contains(d.ServiceCalendarId)).ExecuteDeleteAsync(cancellationToken);

        context.SeasonDays.AddRange(state.Days.Select(d => new DAL.Entities.SeasonDay
        {
            SeasonId = season.Id,
            Date = d.Date,
            Mode = (short)d.Mode,
            CalendarId = d.CalendarId,
            Source = (short)d.Source,
            Note = d.Note
        }));
        context.CalendarDates.AddRange(calendarDates.Select(d => new CalendarDate
        {
            ServiceCalendarId = d.ServiceCalendarId,
            Date = d.Date,
            ExceptionType = (short)d.ExceptionType
        }));

        await SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Tipovi dana, njihove iznimke i dani sezone brišu se kaskadno.
            return await context.TimetableSeasons.Where(s => s.Id == id).ExecuteDeleteAsync(cancellationToken) > 0;
        }
        catch (Exception ex) when (ex.GetBaseException() is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation,
            ConstraintName: "fk_trips_service_calendars_service_calendar_id"
        })
        {
            throw new ConflictException("Tipove dana sezone koriste vožnje, pa se sezona ne može obrisati.", ex);
        }
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is PostgresException { ConstraintName: "uq_service_calendars_code" })
        {
            throw new ConflictException("Kalendar s istim kodom već postoji.", ex);
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is PostgresException
        {
            ConstraintName: "fk_trips_service_calendars_service_calendar_id"
        })
        {
            throw new ConflictException("Tip dana koriste vožnje i ne može se obrisati.", ex);
        }
    }

    private static SeasonState ToState(TimetableSeason season) => new(
        ToModel(season),
        season.Calendars
            .Select(ServiceCalendarMapper.ToModel)
            .OrderBy(FirstWeekday)
            .ThenBy(c => c.Name, StringComparer.Ordinal)
            .ToList(),
        season.Days
            .OrderBy(d => d.Date)
            .Select(d => new SeasonDay(d.Date, (SeasonDayMode)d.Mode, d.CalendarId, (SeasonDaySource)d.Source, d.Note))
            .ToList());

    private static TimetableSeasonModel ToModel(TimetableSeason season) => new()
    {
        Id = season.Id,
        Name = season.Name,
        StartDate = season.StartDate,
        EndDate = season.EndDate,
        HolidayCountry = season.HolidayCountry,
        HolidayMode = (SeasonDayMode)season.HolidayMode,
        HolidayCalendarId = season.HolidayCalendarId,
        CreatedAt = season.CreatedAt,
        UpdatedAt = season.UpdatedAt
    };

    /// <summary>Radni dan prije subote, subota prije nedjelje, tipovi bez dana na kraj.</summary>
    private static int FirstWeekday(IServiceCalendarModel c)
    {
        bool[] days = [c.Monday, c.Tuesday, c.Wednesday, c.Thursday, c.Friday, c.Saturday, c.Sunday];
        var first = Array.IndexOf(days, true);
        return first >= 0 ? first : 7;
    }
}
