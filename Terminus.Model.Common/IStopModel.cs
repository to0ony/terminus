using Terminus.Model.Common.Enums;

namespace Terminus.Model.Common;

public interface IStopModel
{
    Guid Id { get; set; }
    Guid AgencyId { get; set; }

    /// <summary>GTFS stop_id, jedinstven unutar agencije.</summary>
    string Code { get; set; }

    string Name { get; set; }
    string? Description { get; set; }
    double Latitude { get; set; }
    double Longitude { get; set; }
    LocationType LocationType { get; set; }
    Guid? ParentStationId { get; set; }
    string? PlatformCode { get; set; }
    WheelchairBoarding WheelchairBoarding { get; set; }
    bool IsActive { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
