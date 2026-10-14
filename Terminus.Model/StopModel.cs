using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Model;

public class StopModel : IStopModel
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public LocationType LocationType { get; set; }
    public Guid? ParentStationId { get; set; }
    public string? PlatformCode { get; set; }
    public WheelchairBoarding WheelchairBoarding { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
