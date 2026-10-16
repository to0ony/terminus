using Terminus.DAL.Entities;
using Terminus.Model;
using Terminus.Model.Common;

namespace Terminus.Repository.Mapping;

internal static class ShapeMapper
{
    /// <param name="points">Točke trase redom; prazno kad se točke ne učitavaju.</param>
    public static IShapeModel ToModel(Shape entity, int pointCount, IEnumerable<ShapePoint> points) => new ShapeModel
    {
        Id = entity.Id,
        Code = entity.Code,
        PointCount = pointCount,
        Points = points.Select(ToModel).ToList(),
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    public static IShapePointModel ToModel(ShapePoint entity) => new ShapePointModel
    {
        Sequence = entity.Sequence,
        Latitude = entity.Latitude,
        Longitude = entity.Longitude,
        DistTraveled = entity.DistTraveled
    };

    public static ShapePoint ToEntity(Guid shapeId, IShapePointModel model) => new()
    {
        ShapeId = shapeId,
        Sequence = model.Sequence,
        Latitude = model.Latitude,
        Longitude = model.Longitude,
        DistTraveled = model.DistTraveled
    };
}
