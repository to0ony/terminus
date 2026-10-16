using Terminus.Model.Common;

namespace Terminus.Model;

public class ShapeModel : IShapeModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public IReadOnlyList<IShapePointModel> Points { get; set; } = [];
    public int PointCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ShapePointModel : IShapePointModel
{
    public int Sequence { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? DistTraveled { get; set; }
}
