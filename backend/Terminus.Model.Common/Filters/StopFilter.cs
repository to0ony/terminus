using Terminus.Model.Common.Enums;

namespace Terminus.Model.Common.Filters;

public class StopFilter
{
    /// <summary>Traži po nazivu ili kodu (bez obzira na velika/mala slova).</summary>
    public string? Search { get; set; }

    public bool? IsActive { get; set; }
    public LocationType? LocationType { get; set; }
    public Guid? ParentStationId { get; set; }
    public WheelchairBoarding? WheelchairBoarding { get; set; }

    /// <summary>Stanice na kojima staju vožnje (true) ili ne staje nijedna (false); kolodvor po peronima.</summary>
    public bool? HasTrips { get; set; }

    public bool? HasCoordinates { get; set; }

    /// <summary>Samo stanice na kojima staju vožnje zadane linije.</summary>
    public Guid? RouteId { get; set; }

    public StopSortField SortBy { get; set; } = StopSortField.Name;
    public bool SortDescending { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
