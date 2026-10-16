namespace Terminus.Model.Common;

public interface IAgencyModel
{
    Guid Id { get; set; }

    /// <summary>GTFS agency_id, jedinstven.</summary>
    string Code { get; set; }

    string Name { get; set; }
    string Url { get; set; }

    /// <summary>IANA naziv vremenske zone, npr. "Europe/Zagreb".</summary>
    string Timezone { get; set; }

    /// <summary>Jezik (BCP 47), npr. "hr".</summary>
    string Lang { get; set; }

    string? Phone { get; set; }

    /// <summary>Stranica s cijenama karata (GTFS agency_fare_url).</summary>
    string? FareUrl { get; set; }

    string? Email { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
