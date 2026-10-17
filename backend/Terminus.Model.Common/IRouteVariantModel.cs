using Terminus.Model.Common.Enums;

namespace Terminus.Model.Common;

/// <summary>
/// Varijanta linije: redoslijed stanica jednog smjera s vremenima vožnje (npr.
/// glavna, skraćena, iz garaže). Predložak za nove vožnje i mjesto trase.
/// </summary>
public interface IRouteVariantModel
{
    Guid Id { get; set; }
    Guid RouteId { get; set; }
    string Name { get; set; }
    TripDirection? DirectionId { get; set; }

    /// <summary>Natpis smjera za nove vožnje, npr. "Sopot".</summary>
    string? Headsign { get; set; }

    Guid? ShapeId { get; set; }

    /// <summary>Redoslijed prikaza; prva varijanta smjera je glavna.</summary>
    int SortOrder { get; set; }

    VariantSource Source { get; set; }

    /// <summary>Stanice redom vožnje.</summary>
    IReadOnlyList<VariantStop> Stops { get; set; }

    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}

/// <param name="TravelTime">Sekunde od polaska s prethodne stanice do dolaska na ovu; 0 za prvu.</param>
/// <param name="DwellTime">Sekunde stajanja na stanici.</param>
public sealed record VariantStop(Guid StopId, int TravelTime, int DwellTime);
