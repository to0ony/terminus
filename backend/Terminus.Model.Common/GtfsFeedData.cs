namespace Terminus.Model.Common;

/// <summary>
/// Cijeli vozni red instance za upis odjednom (uvoz GTFS-a). Id-evi su već
/// dodijeljeni i veze (npr. Trip.RouteId) pokazuju na njih.
/// </summary>
public sealed class GtfsFeedData
{
    public required IReadOnlyList<IAgencyModel> Agencies { get; init; }
    public required IReadOnlyList<IStopModel> Stops { get; init; }
    public required IReadOnlyList<IRouteModel> Routes { get; init; }
    public required IReadOnlyList<IServiceCalendarModel> Calendars { get; init; }
    public required IReadOnlyList<ICalendarDateModel> CalendarDates { get; init; }

    /// <summary>Trase s točkama.</summary>
    public required IReadOnlyList<IShapeModel> Shapes { get; init; }

    public required IReadOnlyList<ITripModel> Trips { get; init; }

    /// <summary>Vremena svih vožnji (TripId pokazuje na vožnju).</summary>
    public required IReadOnlyList<IStopTimeModel> StopTimes { get; init; }

    /// <summary>Cijene s pravilima.</summary>
    public required IReadOnlyList<IFareModel> Fares { get; init; }

    public IFeedInfoModel? FeedInfo { get; init; }
}
