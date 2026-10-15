using Terminus.Model.Common.Enums;

namespace Terminus.Model.Common.Filters;

public class RouteFilter
{
    /// <summary>Traži po kodu, kratkoj oznaci ili punom nazivu (bez obzira na velika/mala slova).</summary>
    public string? Search { get; set; }

    public Guid? AgencyId { get; set; }
    public bool? IsActive { get; set; }
    public RouteType? RouteType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
