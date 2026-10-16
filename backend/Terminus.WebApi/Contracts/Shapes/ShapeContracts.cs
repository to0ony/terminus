using System.ComponentModel.DataAnnotations;

namespace Terminus.WebApi.Contracts.Shapes;

/// <summary>Tijelo zahtjeva za stvaranje (POST) i potpunu izmjenu (PUT) trase.</summary>
public class ShapeRequest
{
    /// <summary>GTFS shape_id, jedinstven.</summary>
    [Required]
    public string? Code { get; set; }

    /// <summary>Točke redom kojim ih vozilo prolazi; redni brojevi se dodjeljuju prema položaju.</summary>
    [Required]
    public List<ShapePointRequest>? Points { get; set; }
}

public class ShapePointRequest
{
    [Required]
    public double? Latitude { get; set; }

    [Required]
    public double? Longitude { get; set; }

    /// <summary>Prijeđena udaljenost od početka trase (GTFS shape_dist_traveled).</summary>
    public double? DistTraveled { get; set; }
}

/// <summary>Query parametri za popis trasa.</summary>
public class ShapeListQuery
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>Trasa u popisu, bez točaka.</summary>
public record ShapeSummaryResponse(
    Guid Id,
    string Code,
    int PointCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record ShapeResponse(
    Guid Id,
    string Code,
    IReadOnlyList<ShapePointResponse> Points,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record ShapePointResponse(
    int Sequence,
    double Latitude,
    double Longitude,
    double? DistTraveled);
