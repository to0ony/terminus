using Terminus.Model.Common.Enums;

namespace Terminus.WebApi.Contracts.Stops;

/// <summary>Query parametri za tablicu stanica: filtri popisa, dodatni filtri i redoslijed.</summary>
public class StopTableQuery : StopListQuery
{
    public WheelchairBoarding? WheelchairBoarding { get; set; }

    /// <summary>Stanice na kojima staju vožnje (true) ili ne staje nijedna (false).</summary>
    public bool? HasTrips { get; set; }

    public bool? HasCoordinates { get; set; }

    /// <summary>Samo stanice na kojima staju vožnje zadane linije.</summary>
    public Guid? RouteId { get; set; }

    public StopSortField SortBy { get; set; } = StopSortField.Name;
    public bool SortDescending { get; set; }
}
