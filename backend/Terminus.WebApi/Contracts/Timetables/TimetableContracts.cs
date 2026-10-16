using System.ComponentModel.DataAnnotations;
using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Service.Common;
using Terminus.WebApi.Contracts.Routes;

namespace Terminus.WebApi.Contracts.Timetables;

/// <summary>Stanica u voznom redu ili na karti (bez opisnih polja).</summary>
public record TimetableStopResponse(
    Guid Id,
    string Code,
    string? PublicCode,
    string Name,
    double? Latitude,
    double? Longitude,
    LocationType LocationType,
    Guid? ParentStationId,
    string? PlatformCode,
    bool IsActive);

/// <param name="Headsign">Najčešći natpis smjera.</param>
/// <param name="ShapeId">Najčešća trasa (GET /api/shapes/{id}).</param>
public record RoutePatternResponse(
    TripDirection? DirectionId,
    string? Headsign,
    Guid? ShapeId,
    int TripCount,
    IReadOnlyList<TimetableStopResponse> Stops);

/// <summary>Query parametri za vozni red linije.</summary>
public class TimetableQuery
{
    /// <summary>Dan voznog reda (YYYY-MM-DD). Bez datuma i kalendara vrijedi današnji dan.</summary>
    public DateOnly? Date { get; set; }

    /// <summary>Sve vožnje jednog kalendara, umjesto datuma.</summary>
    public Guid? ServiceCalendarId { get; set; }

    /// <summary>Samo jedan smjer (0 ili 1); bez njega sve vožnje.</summary>
    public TripDirection? DirectionId { get; set; }
}

/// <param name="Stops">Redci tablice.</param>
/// <param name="Trips">Stupci tablice, poredani po polasku s prve stanice.</param>
public record RouteTimetableResponse(
    RouteResponse Route,
    DateOnly? Date,
    Guid? ServiceCalendarId,
    TripDirection? DirectionId,
    IReadOnlyList<TimetableStopResponse> Stops,
    IReadOnlyList<TimetableCalendarResponse> Calendars,
    IReadOnlyList<TimetableTripResponse> Trips);

public record TimetableCalendarResponse(Guid Id, string Code, string? Name);

/// <param name="Times">Vrijeme za svaki redak (isti indeks kao Stops); null ako vožnja tamo ne staje.</param>
public record TimetableTripResponse(
    Guid Id,
    string Code,
    string? Headsign,
    string? ShortName,
    TripDirection? DirectionId,
    Guid ServiceCalendarId,
    IReadOnlyList<TimetableTimeResponse?> Times);

/// <param name="ArrivalTime">HH:MM:SS, može biti iznad 24 h.</param>
/// <param name="DepartureTime">HH:MM:SS, može biti iznad 24 h.</param>
/// <param name="Timepoint">false za okvirna vremena.</param>
public record TimetableTimeResponse(string ArrivalTime, string DepartureTime, bool Timepoint, PickupDropOffType PickupType, PickupDropOffType DropOffType);

/// <summary>Query parametri za polaske sa stanice jednog dana.</summary>
public class StopDayDeparturesQuery
{
    /// <summary>Dan voznog reda (YYYY-MM-DD); bez njega današnji.</summary>
    public DateOnly? Date { get; set; }
}

public record StopDayDeparturesResponse(
    TimetableStopResponse Stop,
    DateOnly Date,
    IReadOnlyList<StopDayDepartureResponse> Departures);

/// <param name="RouteName">Kratka oznaka linije, a ako je nema, puni naziv.</param>
/// <param name="DepartureTime">Stvarni trenutak polaska u zoni prijevoznika.</param>
public record StopDayDepartureResponse(
    Guid TripId,
    Guid RouteId,
    string? RouteName,
    string? RouteColor,
    string? RouteTextColor,
    string Headsign,
    Guid StopId,
    string? Platform,
    DateTimeOffset DepartureTime);

/// <summary>Područje karte; rubovi su obavezni.</summary>
public class StopAreaQuery
{
    [Required]
    public double? MinLatitude { get; set; }

    [Required]
    public double? MinLongitude { get; set; }

    [Required]
    public double? MaxLatitude { get; set; }

    [Required]
    public double? MaxLongitude { get; set; }

    public bool? IsActive { get; set; }
}

/// <param name="Truncated">true ako u području ima više stanica od vraćenih; treba približiti kartu.</param>
public record StopsInAreaResponse(IReadOnlyList<TimetableStopResponse> Stops, bool Truncated);

public static class TimetableMappings
{
    public static TimetableStopResponse ToTimetableResponse(this IStopModel stop) => new(
        stop.Id,
        stop.Code,
        stop.PublicCode,
        stop.Name,
        stop.Latitude,
        stop.Longitude,
        stop.LocationType,
        stop.ParentStationId,
        stop.PlatformCode,
        stop.IsActive);

    public static RoutePatternResponse ToResponse(this RoutePattern pattern) => new(
        pattern.DirectionId,
        pattern.Headsign,
        pattern.ShapeId,
        pattern.TripCount,
        pattern.Stops.Select(s => s.ToTimetableResponse()).ToList());

    public static Service.Common.RouteTimetableQuery ToQuery(this TimetableQuery query) =>
        new(query.Date, query.ServiceCalendarId, query.DirectionId);

    public static RouteTimetableResponse ToResponse(this RouteTimetable timetable) => new(
        timetable.Route.ToResponse(),
        timetable.Date,
        timetable.ServiceCalendarId,
        timetable.DirectionId,
        timetable.Stops.Select(s => s.ToTimetableResponse()).ToList(),
        timetable.Calendars.Select(c => new TimetableCalendarResponse(c.Id, c.Code, c.Name)).ToList(),
        timetable.Trips.Select(t => new TimetableTripResponse(
            t.Trip.Id,
            t.Trip.Code,
            t.Trip.Headsign,
            t.Trip.ShortName,
            t.Trip.DirectionId,
            t.Trip.ServiceCalendarId,
            t.Times.Select(st => st is null
                ? null
                : new TimetableTimeResponse(
                    GtfsTime.Format(st.ArrivalTime),
                    GtfsTime.Format(st.DepartureTime),
                    st.Timepoint,
                    st.PickupType,
                    st.DropOffType)).ToList())).ToList());

    public static StopDayDeparturesResponse ToResponse(this StopDayDepartures result) => new(
        result.Stop.ToTimetableResponse(),
        result.Date,
        result.Departures.Select(d => new StopDayDepartureResponse(
            d.TripId,
            d.RouteId,
            d.RouteShortName ?? d.RouteLongName,
            d.RouteColor,
            d.RouteTextColor,
            d.Headsign,
            d.StopId,
            d.PlatformCode,
            d.DepartureTime)).ToList());

    // Rubovi su [Required], pa ih je model binding već provjerio.
    public static GeoBounds ToBounds(this StopAreaQuery query) =>
        new(query.MinLatitude!.Value, query.MinLongitude!.Value, query.MaxLatitude!.Value, query.MaxLongitude!.Value);

    public static StopsInAreaResponse ToResponse(this StopsInArea result) =>
        new(result.Stops.Select(s => s.ToTimetableResponse()).ToList(), result.Truncated);
}
