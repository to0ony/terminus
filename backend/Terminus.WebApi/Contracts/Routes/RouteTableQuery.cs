using Terminus.Model.Common.Enums;

namespace Terminus.WebApi.Contracts.Routes;

/// <summary>Query parametri za tablicu linija: filtri popisa, dodatni filtri i redoslijed.</summary>
public class RouteTableQuery : RouteListQuery
{
    /// <summary>Linije s vožnjama (true) ili bez njih (false).</summary>
    public bool? HasTrips { get; set; }

    /// <summary>Samo linije čije vožnje staju na stanici.</summary>
    public Guid? StopId { get; set; }

    public RouteSortField SortBy { get; set; } = RouteSortField.Default;
    public bool SortDescending { get; set; }
}
