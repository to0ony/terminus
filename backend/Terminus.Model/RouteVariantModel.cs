using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Model;

public class RouteVariantModel : IRouteVariantModel
{
    public Guid Id { get; set; }
    public Guid RouteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public TripDirection? DirectionId { get; set; }
    public string? Headsign { get; set; }
    public Guid? ShapeId { get; set; }
    public int SortOrder { get; set; }
    public VariantSource Source { get; set; }
    public IReadOnlyList<VariantStop> Stops { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
