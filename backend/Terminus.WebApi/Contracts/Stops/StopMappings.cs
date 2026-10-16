using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;

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
