using System.ComponentModel.DataAnnotations;
using Terminus.Model.Common.Enums;

namespace Terminus.WebApi.Contracts.Trips;

/// <summary>Tijelo zahtjeva za stvaranje (POST) i potpunu izmjenu (PUT) vožnje.</summary>
public class TripRequest
{
    [Required]
    public Guid? RouteId { get; set; }

    [Required]
    public Guid? ServiceCalendarId { get; set; }

    [Required]
    public string? Code { get; set; }

    public string? Headsign { get; set; }
    public string? ShortName { get; set; }

    /// <summary>0 ili 1 (smjer tamo/natrag); može se izostaviti.</summary>
    public TripDirection? DirectionId { get; set; }

    public string? BlockId { get; set; }

    /// <summary>Trasa vožnje (GTFS shape_id).</summary>
    public Guid? ShapeId { get; set; }

    public WheelchairAccessible WheelchairAccessible { get; set; } = WheelchairAccessible.Unknown;
    public BikesAllowed BikesAllowed { get; set; } = BikesAllowed.Unknown;
}
