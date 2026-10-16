namespace Terminus.DAL.Entities;

public class Shape : IAuditable
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ShapePoint> Points { get; set; } = new List<ShapePoint>();
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}
