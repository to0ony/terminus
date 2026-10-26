using Terminus.Model.Common.Enums;
using Terminus.Service.Common;
using Terminus.WebApi.Contracts.Timetables;

namespace Terminus.WebApi.Contracts.Routes;

/// <summary>Query parametri za kartu uživo.</summary>
public class LiveMapQuery
{
    /// <summary>Dan voznog reda (YYYY-MM-DD); bez njega današnji u zoni prijevoznika.</summary>
    public DateOnly? Date { get; set; }
}

/// <param name="TimeZone">IANA zona prijevoznika; u njoj se računa trenutno vrijeme.</param>
public record LiveRouteDayResponse(
    RouteResponse Route,
    DateOnly Date,
    string TimeZone,
    IReadOnlyList<LiveTripResponse> Trips,
    IReadOnlyList<LiveShapeResponse> Shapes,
    IReadOnlyList<TimetableStopResponse> Stops);

/// <param name="ShapeId">Trasa iz Shapes; null ili nepoznata: ravno od stanice do stanice.</param>
/// <param name="PreviousDay">Vožnja prethodnog dana koja traje iza ponoći.</param>
public record LiveTripResponse(
    Guid Id,
    string? ShortName,
    string Headsign,
    TripDirection? DirectionId,
    Guid? ShapeId,
    bool PreviousDay,
    IReadOnlyList<LiveStopTimeResponse> StopTimes);

/// <param name="ArrivalTime">Sekunde od ponoći dana karte; negativno za vožnje prethodnog dana.</param>
/// <param name="DepartureTime">Sekunde od ponoći dana karte.</param>
public record LiveStopTimeResponse(Guid StopId, int ArrivalTime, int DepartureTime);

/// <param name="Coordinates">Točke trase redom, [dužina, širina].</param>
public record LiveShapeResponse(Guid Id, IReadOnlyList<double[]> Coordinates);

public static class LiveMapMappings
{
    public static LiveRouteDayResponse ToResponse(this LiveRouteDay day) => new(
        day.Route.ToResponse(),
        day.Date,
        day.TimeZone,
        day.Trips
            .Select(t => new LiveTripResponse(
                t.Id,
                t.ShortName,
                t.Headsign,
                t.DirectionId,
                t.ShapeId,
                t.PreviousDay,
                t.StopTimes.Select(st => new LiveStopTimeResponse(st.StopId, st.ArrivalTime, st.DepartureTime)).ToList()))
            .ToList(),
        day.Shapes
            .Select(s => new LiveShapeResponse(
                s.Id,
                s.Points.OrderBy(p => p.Sequence).Select(p => new[] { p.Longitude, p.Latitude }).ToList()))
            .ToList(),
        day.Stops.Select(s => s.ToTimetableResponse()).ToList());
}
