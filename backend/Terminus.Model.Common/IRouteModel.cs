using Terminus.Model.Common.Enums;

namespace Terminus.Model.Common;

public interface IRouteModel
{
    Guid Id { get; set; }
    Guid AgencyId { get; set; }

    /// <summary>GTFS route_id, jedinstven.</summary>
    string Code { get; set; }

    /// <summary>Kratka oznaka, npr. "12".</summary>
    string? ShortName { get; set; }

    /// <summary>Puni naziv, npr. "Osijek - Đakovo".</summary>
    string? LongName { get; set; }

    string? Description { get; set; }

    /// <summary>Osnovna ili proširena vrijednost (vidi <see cref="RouteTypes"/>).</summary>
    RouteType RouteType { get; set; }

    /// <summary>Web stranica linije (GTFS route_url).</summary>
    string? Url { get; set; }

    /// <summary>Hex boja bez #, npr. "FF0000".</summary>
    string? Color { get; set; }

    /// <summary>Hex boja teksta bez #.</summary>
    string? TextColor { get; set; }

    int? SortOrder { get; set; }

    /// <summary>Null znači da nije zadano (u GTFS-u isto kao <see cref="ContinuousPickupDropOff.None"/>).</summary>
    ContinuousPickupDropOff? ContinuousPickup { get; set; }

    ContinuousPickupDropOff? ContinuousDropOff { get; set; }

    /// <summary>Mreža kojoj linija pripada (GTFS network_id).</summary>
    string? NetworkId { get; set; }

    /// <summary>Točna ili okvirna vremena na međustanicama.</summary>
    TimeMode TimeMode { get; set; }

    bool IsActive { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
