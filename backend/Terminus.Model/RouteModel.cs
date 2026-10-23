using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Model;

public class RouteModel : IRouteModel
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string? LongName { get; set; }
    public string? Description { get; set; }
    public RouteType RouteType { get; set; } = RouteType.Bus;
    public string? Url { get; set; }
    public string? Color { get; set; }
    public string? TextColor { get; set; }
    public int? SortOrder { get; set; }
    public ContinuousPickupDropOff? ContinuousPickup { get; set; }
    public ContinuousPickupDropOff? ContinuousDropOff { get; set; }
    public string? NetworkId { get; set; }
    public TimeMode TimeMode { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
