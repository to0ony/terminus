namespace Terminus.DAL.Entities;

public class ShapePoint
{
    public Guid ShapeId { get; set; }
    public int Sequence { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? DistTraveled { get; set; }

    public Shape Shape { get; set; } = null!;
}
