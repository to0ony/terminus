using Terminus.Model.Common.Enums;

namespace Terminus.Model.Common.Filters;

public class RouteFilter
{
    /// <summary>Traži po kodu, kratkoj oznaci ili punom nazivu (bez obzira na velika/mala slova).</summary>
    public string? Search { get; set; }

    public Guid? AgencyId { get; set; }
    public bool? IsActive { get; set; }
    public RouteType? RouteType { get; set; }

    /// <summary>Linije s vožnjama (true) ili bez njih (false).</summary>
    public bool? HasTrips { get; set; }

    /// <summary>Samo linije čije vožnje staju na stanici (kolodvor: na njegovim peronima).</summary>
    public Guid? StopId { get; set; }

    public RouteSortField SortBy { get; set; } = RouteSortField.Default;
    public bool SortDescending { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
