using System.ComponentModel.DataAnnotations;
using Terminus.Model.Common.Enums;

namespace Terminus.WebApi.Contracts.Routes;

/// <summary>Tijelo zahtjeva za stvaranje (POST) i potpunu izmjenu (PUT) linije.</summary>
public class RouteRequest
{
    /// <summary>Prijevoznik koji vozi liniju (GTFS routes.agency_id).</summary>
    [Required]
    public Guid? AgencyId { get; set; }

    [Required]
    public string? Code { get; set; }

    public string? ShortName { get; set; }
    public string? LongName { get; set; }
    public string? Description { get; set; }

    /// <summary>Osnovna GTFS vrijednost (3 = autobus) ili proširena (100–1799, npr. 700).</summary>
    public RouteType RouteType { get; set; } = RouteType.Bus;

    public string? Url { get; set; }

    /// <summary>Hex boja, s # ili bez njega (npr. "FF0000" ili "#ff0000").</summary>
    public string? Color { get; set; }

    public string? TextColor { get; set; }
    public int? SortOrder { get; set; }

    /// <summary>Ukrcavanje bilo gdje na trasi; izostavljeno znači samo na stanicama.</summary>
    public ContinuousPickupDropOff? ContinuousPickup { get; set; }

    public ContinuousPickupDropOff? ContinuousDropOff { get; set; }
    public string? NetworkId { get; set; }
    public bool IsActive { get; set; } = true;
}
