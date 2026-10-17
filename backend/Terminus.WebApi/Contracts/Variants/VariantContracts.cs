using System.ComponentModel.DataAnnotations;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Service.Common;

namespace Terminus.WebApi.Contracts.Variants;

/// <param name="SortOrder">Redoslijed prikaza; prva varijanta smjera je glavna.</param>
/// <param name="TripCount">Broj vožnji po ovoj varijanti.</param>
public record VariantResponse(
    Guid Id,
    Guid RouteId,
    string Name,
    TripDirection? DirectionId,
    string? Headsign,
    Guid? ShapeId,
    int SortOrder,
    VariantSource Source,
    int TripCount,
    IReadOnlyList<VariantStopResponse> Stops,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <param name="TravelTime">Sekunde od polaska s prethodne stanice; 0 za prvu.</param>
/// <param name="DwellTime">Sekunde stajanja.</param>
public record VariantStopResponse(
    Guid StopId,
    string Code,
    string Name,
    string? PlatformCode,
    double? Latitude,
    double? Longitude,
    int TravelTime,
    int DwellTime);

/// <summary>Tijelo zahtjeva za stvaranje (POST) i potpunu izmjenu (PUT) varijante.</summary>
public class VariantRequest
{
    [Required]
    public string? Name { get; set; }

    public TripDirection? DirectionId { get; set; }
    public string? Headsign { get; set; }
    public Guid? ShapeId { get; set; }

    /// <summary>Stanice redom vožnje.</summary>
    [Required]
    public List<VariantStopRequest>? Stops { get; set; }
}

public class VariantStopRequest
{
    [Required]
    public Guid? StopId { get; set; }

    public int TravelTime { get; set; }
    public int DwellTime { get; set; }
}

public static class VariantMappings
{
    public static IReadOnlyList<VariantResponse> ToResponse(this RouteVariants variants) =>
        variants.Variants
            .Select(v => v.ToResponse(variants.Stops, variants.TripCounts.GetValueOrDefault(v.Id)))
            .ToList();

    public static VariantResponse ToResponse(this RouteVariantDetail detail) =>
        detail.Variant.ToResponse(detail.Stops, detail.TripCount);

    private static VariantResponse ToResponse(this IRouteVariantModel variant, IReadOnlyDictionary<Guid, IStopModel> stops, int tripCount) => new(
        variant.Id,
        variant.RouteId,
        variant.Name,
        variant.DirectionId,
        variant.Headsign,
        variant.ShapeId,
        variant.SortOrder,
        variant.Source,
        tripCount,
        variant.Stops.Select(s =>
        {
            var stop = stops.GetValueOrDefault(s.StopId);
            return new VariantStopResponse(
                s.StopId,
                stop?.Code ?? "",
                stop?.Name ?? "?",
                stop?.PlatformCode,
                stop?.Latitude,
                stop?.Longitude,
                s.TravelTime,
                s.DwellTime);
        }).ToList(),
        variant.CreatedAt,
        variant.UpdatedAt);

    // Name i Stops su [Required], pa ih je model binding već provjerio.
    public static IRouteVariantModel ToModel(this VariantRequest request) => new RouteVariantModel
    {
        Name = request.Name!,
        DirectionId = request.DirectionId,
        Headsign = request.Headsign,
        ShapeId = request.ShapeId,
        Stops = request.Stops!.Select(s => new VariantStop(s.StopId!.Value, s.TravelTime, s.DwellTime)).ToList()
    };
}
