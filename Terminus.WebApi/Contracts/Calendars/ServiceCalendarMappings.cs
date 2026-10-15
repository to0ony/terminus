using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;

namespace Terminus.WebApi.Contracts.Calendars;

public static class ServiceCalendarMappings
{
    // Code, Name, StartDate i EndDate su [Required], pa ih je model binding već provjerio.
    public static IServiceCalendarModel ToModel(this ServiceCalendarRequest request) => new ServiceCalendarModel
    {
        Code = request.Code!,
        Name = request.Name!,
        Monday = request.Monday,
        Tuesday = request.Tuesday,
        Wednesday = request.Wednesday,
        Thursday = request.Thursday,
        Friday = request.Friday,
        Saturday = request.Saturday,
        Sunday = request.Sunday,
        StartDate = request.StartDate!.Value,
        EndDate = request.EndDate!.Value
    };

    public static ServiceCalendarResponse ToResponse(this IServiceCalendarModel model) => new(
        model.Id,
        model.Code,
        model.Name,
        model.Monday,
        model.Tuesday,
        model.Wednesday,
        model.Thursday,
        model.Friday,
        model.Saturday,
        model.Sunday,
        model.StartDate,
        model.EndDate,
        model.CreatedAt,
        model.UpdatedAt);

    public static CalendarDateResponse ToResponse(this ICalendarDateModel model) =>
        new(model.Date, model.ExceptionType);

    public static ServiceCalendarFilter ToFilter(this ServiceCalendarListQuery query) => new()
    {
        Search = query.Search,
        Page = query.Page,
        PageSize = query.PageSize
    };
}
