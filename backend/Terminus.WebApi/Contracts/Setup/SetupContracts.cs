using System.ComponentModel.DataAnnotations;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Service.Common;

namespace Terminus.WebApi.Contracts.Setup;

/// <param name="Completed">true kad je čarobnjak prvog pokretanja završen.</param>
public record SetupStatusResponse(bool Completed, DateTime? CompletedAt, SetupCountsResponse Counts);

public record SetupCountsResponse(int Agencies, int Stops, int Routes, int Trips);

/// <summary>Prvi prijevoznik pri početku od nule.</summary>
public class StartBlankRequest
{
    /// <summary>GTFS agency_id; ako se izostavi, izvodi se iz naziva (npr. "gpp").</summary>
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
    public string? FareUrl { get; set; }
    public string? Email { get; set; }
}

public static class SetupMappings
{
    public static SetupStatusResponse ToResponse(this SetupStatus status) => new(
        status.Completed,
        status.CompletedAt,
        new SetupCountsResponse(status.Counts.Agencies, status.Counts.Stops, status.Counts.Routes, status.Counts.Trips));

    // Name i Url su [Required]; prazan kod, zonu i jezik popunjava servis.
    public static IAgencyModel ToModel(this StartBlankRequest request) => new AgencyModel
    {
        Code = request.Code ?? string.Empty,
        Name = request.Name!,
        Url = request.Url!,
        Timezone = request.Timezone ?? string.Empty,
        Lang = request.Lang ?? string.Empty,
        Phone = request.Phone,
        FareUrl = request.FareUrl,
        Email = request.Email
    };
}
