using System.ComponentModel.DataAnnotations;
using Terminus.Model.Common.Enums;

namespace Terminus.WebApi.Contracts.Stops;

/// <summary>Tijelo zahtjeva za stvaranje (POST) i potpunu izmjenu (PUT) stanice.</summary>
public class StopRequest
{
    [Required]
    public string? Code { get; set; }

    [Required]
    public string? Name { get; set; }

    public string? Description { get; set; }

    [Required]
    public double? Latitude { get; set; }

    [Required]
    public double? Longitude { get; set; }

    public LocationType LocationType { get; set; } = LocationType.StopOrPlatform;
    public Guid? ParentStationId { get; set; }
    public string? PlatformCode { get; set; }
    public WheelchairBoarding WheelchairBoarding { get; set; } = WheelchairBoarding.Unknown;
    public bool IsActive { get; set; } = true;
}
