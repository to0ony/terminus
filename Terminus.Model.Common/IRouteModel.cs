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
    RouteType RouteType { get; set; }

    /// <summary>Hex boja bez #, npr. "FF0000".</summary>
    string? Color { get; set; }

    /// <summary>Hex boja teksta bez #.</summary>
    string? TextColor { get; set; }

    int? SortOrder { get; set; }
    bool IsActive { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
