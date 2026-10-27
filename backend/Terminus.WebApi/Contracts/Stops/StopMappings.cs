using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;
using Terminus.WebApi.Contracts.Routes;

namespace Terminus.WebApi.Contracts.Stops;

public static class StopMappings
{
    // Code i Name su [Required], pa ih model binding već provjerio.
    public static IStopModel ToModel(this StopRequest request) => new StopModel
    {
        Code = request.Code!,
        PublicCode = request.PublicCode,
        Name = request.Name!,
        TtsName = request.TtsName,
        Description = request.Description,
        Latitude = request.Latitude,
        Longitude = request.Longitude,
        ZoneId = request.ZoneId,
        Url = request.Url,
        LocationType = request.LocationType,
        ParentStationId = request.ParentStationId,
        Timezone = request.Timezone,
        PlatformCode = request.PlatformCode,
        WheelchairBoarding = request.WheelchairBoarding,
        IsActive = request.IsActive
    };

    public static StopResponse ToResponse(this IStopModel model) => new(
        model.Id,
        model.Code,
        model.PublicCode,
        model.Name,
        model.TtsName,
        model.Description,
        model.Latitude,
        model.Longitude,
        model.ZoneId,
        model.Url,
        model.LocationType,
        model.ParentStationId,
        model.Timezone,
        model.PlatformCode,
        model.WheelchairBoarding,
        model.IsActive,
        model.CreatedAt,
        model.UpdatedAt);

    public static StopTableRowResponse ToResponse(this StopTableRow row) => new(
        row.Stop.ToResponse(),
        row.ParentStationName,
        row.PlatformCount,
        row.TripCount,
        row.Routes.Select(r => new RouteTagResponse(r.Id, r.Code, r.ShortName, r.Color, r.TextColor)).ToList());

    public static StopFilter ToFilter(this StopTableQuery query)
    {
        var filter = ((StopListQuery)query).ToFilter();
        filter.WheelchairBoarding = query.WheelchairBoarding;
        filter.HasTrips = query.HasTrips;
        filter.HasCoordinates = query.HasCoordinates;
        filter.RouteId = query.RouteId;
        filter.SortBy = query.SortBy;
        filter.SortDescending = query.SortDescending;
        return filter;
    }

    public static StopFilter ToFilter(this StopListQuery query) => new()
    {
        Search = query.Search,
        IsActive = query.IsActive,
        LocationType = query.LocationType,
        ParentStationId = query.ParentStationId,
        Page = query.Page,
        PageSize = query.PageSize
    };
}
