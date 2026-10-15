using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;

namespace Terminus.WebApi.Contracts.Stops;

public static class StopMappings
{
    // Code, Name, Latitude i Longitude su [Required], pa ih model binding već provjerio.
    public static IStopModel ToModel(this StopRequest request) => new StopModel
    {
        Code = request.Code!,
        Name = request.Name!,
        Description = request.Description,
        Latitude = request.Latitude!.Value,
        Longitude = request.Longitude!.Value,
        LocationType = request.LocationType,
        ParentStationId = request.ParentStationId,
        PlatformCode = request.PlatformCode,
        WheelchairBoarding = request.WheelchairBoarding,
        IsActive = request.IsActive
    };

    public static StopResponse ToResponse(this IStopModel model) => new(
        model.Id,
        model.Code,
        model.Name,
        model.Description,
        model.Latitude,
        model.Longitude,
        model.LocationType,
        model.ParentStationId,
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
