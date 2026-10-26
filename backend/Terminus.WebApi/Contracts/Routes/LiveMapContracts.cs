using Terminus.Model.Common;
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

/// <summary>Sva vozila: vožnje svih linija dana.</summary>
public record LiveNetworkDayResponse(
    DateOnly Date,
    string TimeZone,
    IReadOnlyList<RouteResponse> Routes,
    IReadOnlyList<LiveTripResponse> Trips,
    IReadOnlyList<LiveShapeResponse> Shapes,
    IReadOnlyList<TimetableStopResponse> Stops);

/// <param name="ShapeId">Trasa iz Shapes; null ili nepoznata: ravno od stanice do stanice.</param>
/// <param name="PreviousDay">Vožnja prethodnog dana koja traje iza ponoći.</param>
public record LiveTripResponse(
    Guid Id,
    Guid RouteId,
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
        day.Trips.Select(ToResponse).ToList(),
        day.Shapes.Select(ToResponse).ToList(),
        day.Stops.Select(s => s.ToTimetableResponse()).ToList());

    public static LiveNetworkDayResponse ToResponse(this LiveNetworkDay day) => new(
        day.Date,
        day.TimeZone,
        day.Routes.Select(r => r.ToResponse()).ToList(),
        day.Trips.Select(ToResponse).ToList(),
        day.Shapes.Select(ToResponse).ToList(),
        day.Stops.Select(s => s.ToTimetableResponse()).ToList());

    private static LiveTripResponse ToResponse(LiveTrip trip) => new(
        trip.Id,
        trip.RouteId,
        trip.ShortName,
        trip.Headsign,
        trip.DirectionId,
        trip.ShapeId,
        trip.PreviousDay,
        trip.StopTimes.Select(st => new LiveStopTimeResponse(st.StopId, st.ArrivalTime, st.DepartureTime)).ToList());

    // Šest decimala je oko 10 cm; kraći odgovor za trase svih linija.
    private static LiveShapeResponse ToResponse(IShapeModel shape) => new(
        shape.Id,
        shape.Points
            .OrderBy(p => p.Sequence)
            .Select(p => new[] { Math.Round(p.Longitude, 6), Math.Round(p.Latitude, 6) })
            .ToList());
}
