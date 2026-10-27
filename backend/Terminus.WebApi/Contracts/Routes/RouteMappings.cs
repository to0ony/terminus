using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;

namespace Terminus.WebApi.Contracts.Routes;

public static class RouteMappings
{
    // AgencyId i Code su [Required], pa ih je model binding već provjerio.
    public static IRouteModel ToModel(this RouteRequest request) => new RouteModel
    {
        AgencyId = request.AgencyId!.Value,
        Code = request.Code!,
        ShortName = request.ShortName,
        LongName = request.LongName,
        Description = request.Description,
        RouteType = request.RouteType,
        Url = request.Url,
        Color = request.Color,
        TextColor = request.TextColor,
        SortOrder = request.SortOrder,
        ContinuousPickup = request.ContinuousPickup,
        ContinuousDropOff = request.ContinuousDropOff,
        NetworkId = request.NetworkId,
        TimeMode = request.TimeMode,
        IsActive = request.IsActive
    };

    public static RouteResponse ToResponse(this IRouteModel model) => new(
        model.Id,
        model.AgencyId,
        model.Code,
        model.ShortName,
        model.LongName,
        model.Description,
        model.RouteType,
        model.Url,
        model.Color,
        model.TextColor,
        model.SortOrder,
        model.ContinuousPickup,
        model.ContinuousDropOff,
        model.NetworkId,
        model.TimeMode,
        model.IsActive,
        model.CreatedAt,
        model.UpdatedAt);

    public static RouteTableRowResponse ToResponse(this RouteTableRow row) => new(
        row.Route.ToResponse(),
        row.AgencyName,
        row.VariantCount,
        row.ValidFrom,
        row.ValidTo);

    public static RouteFilter ToFilter(this RouteTableQuery query)
    {
        var filter = ((RouteListQuery)query).ToFilter();
        filter.HasTrips = query.HasTrips;
        filter.StopId = query.StopId;
        filter.SortBy = query.SortBy;
        filter.SortDescending = query.SortDescending;
        return filter;
    }

    public static RouteFilter ToFilter(this RouteListQuery query) => new()
    {
        Search = query.Search,
        AgencyId = query.AgencyId,
        IsActive = query.IsActive,
        RouteType = query.RouteType,
        Page = query.Page,
        PageSize = query.PageSize
    };
}
