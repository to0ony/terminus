using Terminus.Model.Common.Enums;

namespace Terminus.Model.Common;

public interface ITripModel
{
    Guid Id { get; set; }
    Guid RouteId { get; set; }
    Guid ServiceCalendarId { get; set; }

    /// <summary>GTFS trip_id, jedinstven.</summary>
    string Code { get; set; }

    /// <summary>Natpis smjera na vozilu, npr. "Đakovo".</summary>
    string? Headsign { get; set; }

    string? ShortName { get; set; }
    TripDirection? DirectionId { get; set; }

    /// <summary>Vožnje s istim BlockId vozi isto vozilo.</summary>
    string? BlockId { get; set; }

    /// <summary>Trasa vožnje (GTFS shape_id).</summary>
    Guid? ShapeId { get; set; }

    /// <summary>Varijanta linije po kojoj vožnja vozi; nije dio GTFS-a.</summary>
    Guid? VariantId { get; set; }


    WheelchairAccessible WheelchairAccessible { get; set; }
    BikesAllowed BikesAllowed { get; set; }
    /// <summary>Prva i zadnja stanica s vremenima; puni se samo pri čitanju, null kod upisa.</summary>
    TripSummary? Summary { get; set; }

    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
