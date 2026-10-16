namespace Terminus.Model.Common;

/// <summary>Trasa vožnje na karti (GTFS shapes.txt).</summary>
public interface IShapeModel
{
    Guid Id { get; set; }

    /// <summary>GTFS shape_id, jedinstven.</summary>
    string Code { get; set; }

    /// <summary>Točke trase redom. Popis trasa vraća ih prazne; broj je u <see cref="PointCount"/>.</summary>
    IReadOnlyList<IShapePointModel> Points { get; set; }

    int PointCount { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}

/// <summary>Jedna točka trase.</summary>
public interface IShapePointModel
{
    /// <summary>Redni broj točke (1, 2, 3 ...).</summary>
    int Sequence { get; set; }

    double Latitude { get; set; }
    double Longitude { get; set; }

    /// <summary>Prijeđena udaljenost od početka trase (GTFS shape_dist_traveled).</summary>
    double? DistTraveled { get; set; }
}
