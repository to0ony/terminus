using System.ComponentModel.DataAnnotations;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Service.Common;

namespace Terminus.WebApi.Contracts.Routes;

/// <summary>Query parametri grafikona voznog reda.</summary>
public class ScheduleQuery
{
    /// <summary>Smjer (0 ili 1); bez njega vožnje bez smjera i oba smjera zajedno.</summary>
    public TripDirection? DirectionId { get; set; }

    /// <summary>Samo vožnje tog kalendara (tipa dana).</summary>
    public Guid? CalendarId { get; set; }
}

/// <param name="Stops">Redci grafikona, redom kojim ih varijante i vožnje obilaze.</param>
/// <param name="Variants">Varijante smjera (glavna prva): predlošci za nove vožnje.</param>
/// <param name="TripCounts">Broj vožnji smjera po Id-u kalendara.</param>
public record ScheduleResponse(
    Guid RouteId,
    TripDirection? DirectionId,
    IReadOnlyList<ScheduleStopResponse> Stops,
    IReadOnlyList<ScheduleVariantResponse> Variants,
    IReadOnlyList<ScheduleTripResponse> Trips,
    IReadOnlyDictionary<Guid, int> TripCounts);

/// <param name="TripCount">Broj vožnji smjera (svih kalendara) po ovoj varijanti.</param>
public record ScheduleVariantResponse(
    Guid Id,
    string Name,
    TripDirection? DirectionId,
    string? Headsign,
    Guid? ShapeId,
    int SortOrder,
    int TripCount,
    IReadOnlyList<ScheduleVariantStopResponse> Stops);

/// <param name="TravelTime">Sekunde od polaska s prethodne stanice; 0 za prvu.</param>
/// <param name="DwellTime">Sekunde stajanja.</param>
public record ScheduleVariantStopResponse(Guid StopId, int TravelTime, int DwellTime);

public record ScheduleStopResponse(Guid Id, string Code, string Name, string? PlatformCode);

/// <param name="UpdatedAt">Vraća se pri spremanju izmjena, za provjeru sukoba.</param>
public record ScheduleTripResponse(
    Guid Id,
    string Code,
    Guid ServiceCalendarId,
    string? Headsign,
    string? ShortName,
    string? BlockId,
    Guid? ShapeId,
    Guid? VariantId,
    WheelchairAccessible WheelchairAccessible,
    BikesAllowed BikesAllowed,
    DateTime UpdatedAt,
    IReadOnlyList<ScheduleStopTimeResponse> StopTimes,
    Guid? DepartureId,
    IReadOnlyList<Guid> DayTypes);

/// <param name="ArrivalTime">Sekunde od početka dana voznog reda (može biti iznad 24 h).</param>
/// <param name="DepartureTime">Sekunde od početka dana voznog reda (može biti iznad 24 h).</param>
public record ScheduleStopTimeResponse(
    Guid StopId,
    int ArrivalTime,
    int DepartureTime,
    string? StopHeadsign,
    PickupDropOffType PickupType,
    PickupDropOffType DropOffType,
    ContinuousPickupDropOff? ContinuousPickup,
    ContinuousPickupDropOff? ContinuousDropOff,
    double? ShapeDistTraveled,
    bool Timepoint);

/// <summary>Izmjene iz grafikona: nove i izmijenjene vožnje te obrisane.</summary>
public class ScheduleSaveRequest
{
    public TripDirection? DirectionId { get; set; }

    [Required]
    public List<ScheduleTripRequest>? Upserts { get; set; }

    [Required]
    public List<ScheduleDeleteRequest>? Deletes { get; set; }
}

/// <summary>Vožnja; bez Id-a je nova, a postojeća šalje UpdatedAt koji je učitala.</summary>
public class ScheduleTripRequest
{
    public Guid? Id { get; set; }
    public DateTime? UpdatedAt { get; set; }

    [Required]
    public Guid? ServiceCalendarId { get; set; }

    public string? Headsign { get; set; }
    public string? ShortName { get; set; }
    public string? BlockId { get; set; }
    public Guid? ShapeId { get; set; }

    /// <summary>Varijanta linije po kojoj vožnja vozi.</summary>
    public Guid? VariantId { get; set; }

    public WheelchairAccessible WheelchairAccessible { get; set; }
    public BikesAllowed BikesAllowed { get; set; }

    /// <summary>Vremena redom vožnje.</summary>
    [Required]
    public List<ScheduleStopTimeRequest>? StopTimes { get; set; }

    /// <summary>
    /// Svi tipovi dana u kojima polazak vozi (uz ServiceCalendarId); vožnje u ostalima se
    /// dodaju, usklađuju ili brišu. Bez njega polazak ostaje u istim tipovima dana.
    /// </summary>
    public List<Guid>? DayTypes { get; set; }

    /// <summary>Vožnja izlazi iz polaska; vožnje ostalih tipova dana ostaju.</summary>
    public bool Detach { get; set; }
}

public class ScheduleStopTimeRequest
{
    [Required]
    public Guid? StopId { get; set; }

    public int ArrivalTime { get; set; }
    public int DepartureTime { get; set; }
    public string? StopHeadsign { get; set; }
    public PickupDropOffType PickupType { get; set; }
    public PickupDropOffType DropOffType { get; set; }
    public ContinuousPickupDropOff? ContinuousPickup { get; set; }
    public ContinuousPickupDropOff? ContinuousDropOff { get; set; }
    public double? ShapeDistTraveled { get; set; }
    public bool Timepoint { get; set; } = true;
}

public class ScheduleDeleteRequest
{
    [Required]
    public Guid? Id { get; set; }

    [Required]
    public DateTime? UpdatedAt { get; set; }
}

public static class ScheduleMappings
{
    public static ScheduleResponse ToResponse(this RouteSchedule schedule) => new(
        schedule.Route.Id,
        schedule.DirectionId,
        schedule.Stops.Select(s => new ScheduleStopResponse(s.Id, s.Code, s.Name, s.PlatformCode)).ToList(),
        schedule.Variants.Select(v => new ScheduleVariantResponse(
            v.Id,
            v.Name,
            v.DirectionId,
            v.Headsign,
            v.ShapeId,
            v.SortOrder,
            schedule.VariantTripCounts.GetValueOrDefault(v.Id),
            v.Stops.Select(s => new ScheduleVariantStopResponse(s.StopId, s.TravelTime, s.DwellTime)).ToList())).ToList(),
        schedule.Trips.Select(t => new ScheduleTripResponse(
            t.Trip.Id,
            t.Trip.Code,
            t.Trip.ServiceCalendarId,
            t.Trip.Headsign,
            t.Trip.ShortName,
            t.Trip.BlockId,
            t.Trip.ShapeId,
            t.Trip.VariantId,
            t.Trip.WheelchairAccessible,
            t.Trip.BikesAllowed,
            t.Trip.UpdatedAt,
            t.StopTimes.Select(st => new ScheduleStopTimeResponse(
                st.StopId,
                st.ArrivalTime,
                st.DepartureTime,
                st.StopHeadsign,
                st.PickupType,
                st.DropOffType,
                st.ContinuousPickup,
                st.ContinuousDropOff,
                st.ShapeDistTraveled,
                st.Timepoint)).ToList(),
            t.Trip.DepartureId,
            t.Trip.DepartureId is { } departure && schedule.DepartureDayTypes.TryGetValue(departure, out var dayTypes)
                ? dayTypes
                : [t.Trip.ServiceCalendarId])).ToList(),
        schedule.TripCounts);

    // Obavezna polja ([Required]) je model binding već provjerio.
    public static ScheduleChanges ToChanges(this ScheduleSaveRequest request) => new(
        request.DirectionId,
        request.Upserts!.Select(u => new ScheduleTripChange(
            new TripModel
            {
                Id = u.Id ?? Guid.Empty,
                ServiceCalendarId = u.ServiceCalendarId!.Value,
                Headsign = u.Headsign,
                ShortName = u.ShortName,
                BlockId = u.BlockId,
                ShapeId = u.ShapeId,
                VariantId = u.VariantId,
                WheelchairAccessible = u.WheelchairAccessible,
                BikesAllowed = u.BikesAllowed
            },
            u.StopTimes!.Select(st => (IStopTimeModel)new StopTimeModel
            {
                StopId = st.StopId!.Value,
                ArrivalTime = st.ArrivalTime,
                DepartureTime = st.DepartureTime,
                StopHeadsign = st.StopHeadsign,
                PickupType = st.PickupType,
                DropOffType = st.DropOffType,
                ContinuousPickup = st.ContinuousPickup,
                ContinuousDropOff = st.ContinuousDropOff,
                ShapeDistTraveled = st.ShapeDistTraveled,
                Timepoint = st.Timepoint
            }).ToList(),
            u.UpdatedAt,
            u.DayTypes,
            u.Detach)).ToList(),
        request.Deletes!.Select(d => new TripVersion(d.Id!.Value, d.UpdatedAt!.Value)).ToList());
}
