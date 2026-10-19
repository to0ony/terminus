using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Service.Common;

namespace Terminus.WebApi.Contracts.Seasons;

public static class SeasonMappings
{
    public static SeasonResponse ToResponse(this SeasonDetail detail)
    {
        var season = detail.State.Season;
        return new SeasonResponse(
            season.Id,
            season.Name,
            season.StartDate,
            season.EndDate,
            season.HolidayCountry,
            season.HolidayMode,
            season.HolidayCalendarId,
            detail.State.DayTypes
                .Select(d => new DayTypeResponse(
                    d.Id, d.Code, d.Name,
                    d.Monday, d.Tuesday, d.Wednesday, d.Thursday, d.Friday, d.Saturday, d.Sunday,
                    detail.TripCounts.GetValueOrDefault(d.Id)))
                .ToList(),
            detail.State.Days
                .Select(d => new SeasonDayResponse(d.Date, d.Mode, d.CalendarId, d.Source, d.Note))
                .ToList(),
            detail.State.Periods
                .Select(p => new SeasonPeriodResponse(
                    p.Id, p.Name, p.StartDate, p.EndDate, p.ReplacedCalendarId, p.Mode, p.CalendarId))
                .ToList(),
            season.CreatedAt,
            season.UpdatedAt);
    }

    // Name, StartDate i EndDate su [Required], pa ih je model binding već provjerio.
    public static ITimetableSeasonModel ToModel(this CreateSeasonRequest request) => new TimetableSeasonModel
    {
        Name = request.Name!,
        StartDate = request.StartDate!.Value,
        EndDate = request.EndDate!.Value,
        HolidayCountry = request.HolidayCountry,
        HolidayMode = request.HolidayMode
    };

    public static ITimetableSeasonModel ToModel(this CopySeasonRequest request) => new TimetableSeasonModel
    {
        Name = request.Name!,
        StartDate = request.StartDate!.Value,
        EndDate = request.EndDate!.Value
    };

    public static ITimetableSeasonModel ToModel(this UpdateSeasonRequest request) => new TimetableSeasonModel
    {
        Name = request.Name!,
        StartDate = request.StartDate!.Value,
        EndDate = request.EndDate!.Value,
        HolidayCountry = request.HolidayCountry,
        HolidayMode = request.HolidayMode,
        HolidayCalendarId = request.HolidayCalendarId
    };

    // Name, datumi i ReplacedCalendarId su [Required].
    public static IReadOnlyList<SeasonPeriod> ToModels(this IEnumerable<SeasonPeriodRequest> periods) =>
        periods.Select(p => new SeasonPeriod(
            p.Id ?? Guid.Empty,
            p.Name!,
            p.StartDate!.Value,
            p.EndDate!.Value,
            p.ReplacedCalendarId!.Value,
            p.Mode,
            p.CalendarId)).ToList();

    public static IReadOnlyList<IServiceCalendarModel> ToModels(this IEnumerable<DayTypeRequest> dayTypes) =>
        dayTypes.Select(d => (IServiceCalendarModel)new ServiceCalendarModel
        {
            Id = d.Id ?? Guid.Empty,
            Name = d.Name!,
            Monday = d.Monday,
            Tuesday = d.Tuesday,
            Wednesday = d.Wednesday,
            Thursday = d.Thursday,
            Friday = d.Friday,
            Saturday = d.Saturday,
            Sunday = d.Sunday
        }).ToList();

    public static HolidayCountriesResponse ToResponse(this HolidayCountries countries) => new(
        countries.Countries.Select(c => new HolidayCountryResponse(c.Code, c.Name)).ToList(),
        countries.Suggested);
}
