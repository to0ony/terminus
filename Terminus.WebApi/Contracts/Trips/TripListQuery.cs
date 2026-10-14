using Terminus.Model.Common.Enums;

namespace Terminus.WebApi.Contracts.Trips;

/// <summary>Query parametri za popis vožnji.</summary>
public class TripListQuery
{
    public string? Search { get; set; }
    public Guid? RouteId { get; set; }
    public Guid? ServiceCalendarId { get; set; }
    public TripDirection? DirectionId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
