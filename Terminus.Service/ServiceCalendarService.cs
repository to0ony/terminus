using Terminus.Common;
using Terminus.Common.Exceptions;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Model.Common.Filters;
using Terminus.Repository.Common;
using Terminus.Service.Common;

namespace Terminus.Service;

public class ServiceCalendarService(
    IServiceCalendarRepository calendarRepository,
    ICalendarDateRepository calendarDateRepository) : IServiceCalendarService
{
    public async Task<IServiceCalendarModel> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        await calendarRepository.GetAsync(id, cancellationToken) ?? throw CalendarNotFound(id);

    public Task<PagedResult<IServiceCalendarModel>> FindAsync(ServiceCalendarFilter filter, CancellationToken cancellationToken = default)
    {
        var errors = new ValidationErrors();
        errors.AddPagingErrors(filter.Page, filter.PageSize);
        errors.ThrowIfAny();

        return calendarRepository.FindAsync(filter, cancellationToken);
    }

    public async Task<IServiceCalendarModel> CreateAsync(IServiceCalendarModel calendar, CancellationToken cancellationToken = default)
    {
        calendar.Id = Guid.Empty;
        Normalize(calendar);
        Validate(calendar);

        return await calendarRepository.InsertAsync(calendar, cancellationToken);
    }

    public async Task<IServiceCalendarModel> UpdateAsync(Guid id, IServiceCalendarModel calendar, CancellationToken cancellationToken = default)
    {
        calendar.Id = id;
        Normalize(calendar);
        Validate(calendar);

        return await calendarRepository.UpdateAsync(calendar, cancellationToken) ?? throw CalendarNotFound(id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await calendarRepository.DeleteAsync(id, cancellationToken))
        {
            throw CalendarNotFound(id);
        }
    }

    public async Task<IReadOnlyList<ICalendarDateModel>> GetDatesAsync(Guid calendarId, CancellationToken cancellationToken = default)
    {
        await EnsureCalendarExistsAsync(calendarId, cancellationToken);
        return await calendarDateRepository.GetByCalendarAsync(calendarId, cancellationToken);
    }

    public async Task<ICalendarDateModel> SetDateAsync(
        Guid calendarId,
        DateOnly date,
        CalendarExceptionType exceptionType,
        CancellationToken cancellationToken = default)
    {
        await EnsureCalendarExistsAsync(calendarId, cancellationToken);

        if (!Enum.IsDefined(exceptionType))
        {
            var errors = new ValidationErrors();
            errors.Add("ExceptionType", "Vrsta iznimke mora biti 1 (vozi) ili 2 (ne vozi).");
            errors.ThrowIfAny();
        }

        // Datum smije biti i izvan razdoblja kalendara: GTFS tako dodaje pojedinačne dane vožnje.
        return await calendarDateRepository.UpsertAsync(new CalendarDateModel
        {
            ServiceCalendarId = calendarId,
            Date = date,
            ExceptionType = exceptionType
        }, cancellationToken);
    }

    public async Task RemoveDateAsync(Guid calendarId, DateOnly date, CancellationToken cancellationToken = default)
    {
        await EnsureCalendarExistsAsync(calendarId, cancellationToken);

        if (!await calendarDateRepository.DeleteAsync(calendarId, date, cancellationToken))
        {
            throw new NotFoundException($"Kalendar nema iznimku za {date:yyyy-MM-dd}.");
        }
    }

    /// <summary>Nepostojeći kalendar je 404, a ne prazan popis iznimaka.</summary>
    private async Task EnsureCalendarExistsAsync(Guid calendarId, CancellationToken cancellationToken)
    {
        if (await calendarRepository.GetAsync(calendarId, cancellationToken) is null)
        {
            throw CalendarNotFound(calendarId);
        }
    }

    private static void Normalize(IServiceCalendarModel calendar)
    {
        calendar.Code = calendar.Code?.Trim() ?? string.Empty;
        calendar.Name = calendar.Name?.Trim() ?? string.Empty;
    }

    private static void Validate(IServiceCalendarModel calendar)
    {
        var errors = new ValidationErrors();

        if (calendar.Code.Length == 0)
        {
            errors.Add(nameof(calendar.Code), "Kod je obavezan.");
        }

        if (calendar.Name.Length == 0)
        {
            errors.Add(nameof(calendar.Name), "Naziv je obavezan.");
        }

        if (calendar.EndDate < calendar.StartDate)
        {
            errors.Add(nameof(calendar.EndDate), "Završni datum ne smije biti prije početnog.");
        }

        errors.ThrowIfAny();
    }

    private static NotFoundException CalendarNotFound(Guid id) => new($"Kalendar {id} ne postoji.");
}
