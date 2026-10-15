using Terminus.Model.Common.Enums;

namespace Terminus.Model.Common.Filters;

public class StopFilter
{
    /// <summary>Traži po nazivu ili kodu (bez obzira na velika/mala slova).</summary>
    public string? Search { get; set; }

    public bool? IsActive { get; set; }
    public LocationType? LocationType { get; set; }
    public Guid? ParentStationId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
