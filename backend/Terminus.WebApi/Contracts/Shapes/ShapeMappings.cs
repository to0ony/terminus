using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;

namespace Terminus.WebApi.Contracts.Shapes;

public static class ShapeMappings
{
    // Code, Points i koordinate točaka su [Required], pa ih je model binding već provjerio.
    public static IShapeModel ToModel(this ShapeRequest request) => new ShapeModel
    {
        Code = request.Code!,
        Points = request.Points!
            .Select(p => (IShapePointModel)new ShapePointModel
            {
                Latitude = p.Latitude!.Value,
                Longitude = p.Longitude!.Value,
                DistTraveled = p.DistTraveled
            })
            .ToList()
    };

    public static ShapeSummaryResponse ToSummaryResponse(this IShapeModel model) => new(
        model.Id,
        model.Code,
        model.PointCount,
        model.CreatedAt,
        model.UpdatedAt);

    public static ShapeResponse ToResponse(this IShapeModel model) => new(
        model.Id,
        model.Code,
        model.Points.Select(p => new ShapePointResponse(p.Sequence, p.Latitude, p.Longitude, p.DistTraveled)).ToList(),
        model.CreatedAt,
        model.UpdatedAt);

    public static ShapeFilter ToFilter(this ShapeListQuery query) => new()
    {
        Search = query.Search,
        Page = query.Page,
        PageSize = query.PageSize
    };
}
