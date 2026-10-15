using Terminus.Model.Common.Enums;

namespace Terminus.WebApi.Contracts.Stops;

/// <summary>Query parametri za popis stanica.</summary>
public class StopListQuery
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
    public LocationType? LocationType { get; set; }
    public Guid? ParentStationId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
