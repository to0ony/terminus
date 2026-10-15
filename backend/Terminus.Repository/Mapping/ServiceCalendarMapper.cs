using Terminus.DAL.Entities;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Repository.Mapping;

internal static class ServiceCalendarMapper
{
    public static IServiceCalendarModel ToModel(ServiceCalendar entity) => new ServiceCalendarModel
    {
        Id = entity.Id,
        Code = entity.Code,
        Name = entity.Name,
        Monday = entity.Monday,
        Tuesday = entity.Tuesday,
        Wednesday = entity.Wednesday,
        Thursday = entity.Thursday,
        Friday = entity.Friday,
        Saturday = entity.Saturday,
        Sunday = entity.Sunday,
        StartDate = entity.StartDate,
        EndDate = entity.EndDate,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    /// <summary>Prepisuje polja koja korisnik smije mijenjati (bez Id-a i vremenskih oznaka).</summary>
    public static void Apply(IServiceCalendarModel model, ServiceCalendar entity)
    {
        entity.Code = model.Code;
        entity.Name = model.Name;
        entity.Monday = model.Monday;
        entity.Tuesday = model.Tuesday;
        entity.Wednesday = model.Wednesday;
        entity.Thursday = model.Thursday;
        entity.Friday = model.Friday;
        entity.Saturday = model.Saturday;
        entity.Sunday = model.Sunday;
        entity.StartDate = model.StartDate;
        entity.EndDate = model.EndDate;
    }

    public static ICalendarDateModel ToModel(CalendarDate entity) => new CalendarDateModel
    {
        ServiceCalendarId = entity.ServiceCalendarId,
        Date = entity.Date,
        ExceptionType = (CalendarExceptionType)entity.ExceptionType
    };
}
