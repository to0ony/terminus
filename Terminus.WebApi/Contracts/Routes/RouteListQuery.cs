using Terminus.Model.Common.Enums;

namespace Terminus.WebApi.Contracts.Routes;

/// <summary>Query parametri za popis linija.</summary>
public class RouteListQuery
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
    public RouteType? RouteType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
