using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Model;

public class TripModel : ITripModel
{
    public Guid Id { get; set; }
    public Guid RouteId { get; set; }
    public Guid ServiceCalendarId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Headsign { get; set; }
    public string? ShortName { get; set; }
    public TripDirection? DirectionId { get; set; }
    public string? BlockId { get; set; }
    public WheelchairAccessible WheelchairAccessible { get; set; }
    public BikesAllowed BikesAllowed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
