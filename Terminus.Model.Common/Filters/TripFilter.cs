using Terminus.Model.Common.Enums;

namespace Terminus.Model.Common.Filters;

public class TripFilter
{
    /// <summary>Traži po kodu ili natpisu smjera (bez obzira na velika/mala slova).</summary>
    public string? Search { get; set; }

    public Guid? RouteId { get; set; }
    public Guid? ServiceCalendarId { get; set; }
    public TripDirection? DirectionId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
