using System.ComponentModel.DataAnnotations;

namespace Terminus.WebApi.Contracts.Agencies;

/// <summary>Tijelo zahtjeva za stvaranje (POST) i potpunu izmjenu (PUT) agencije.</summary>
public class AgencyRequest
{
    /// <summary>GTFS agency_id, jedinstven.</summary>
    [Required]
    public string? Code { get; set; }

    [Required]
    public string? Name { get; set; }

    [Required]
    public string? Url { get; set; }

    /// <summary>IANA vremenska zona. Ako se izostavi, koristi se Europe/Zagreb.</summary>
    public string? Timezone { get; set; }

    /// <summary>Jezik. Ako se izostavi, koristi se hr.</summary>
    public string? Lang { get; set; }

    public string? Phone { get; set; }

    /// <summary>Stranica s cijenama karata.</summary>
    public string? FareUrl { get; set; }

    public string? Email { get; set; }
}
