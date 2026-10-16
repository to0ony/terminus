using System.ComponentModel.DataAnnotations;
using Terminus.Model.Common.Enums;

namespace Terminus.WebApi.Contracts.Stops;

/// <summary>Tijelo zahtjeva za stvaranje (POST) i potpunu izmjenu (PUT) stanice.</summary>
public class StopRequest
{
    /// <summary>GTFS stop_id, jedinstven.</summary>
    [Required]
    public string? Code { get; set; }

    /// <summary>Oznaka za putnike (GTFS stop_code), npr. broj na stupu.</summary>
    public string? PublicCode { get; set; }

    [Required]
    public string? Name { get; set; }

    /// <summary>Naziv za čitače zaslona (GTFS tts_stop_name).</summary>
    public string? TtsName { get; set; }

    public string? Description { get; set; }

    /// <summary>Obavezno osim za čvorove i mjesta ukrcavanja (LocationType 3 i 4).</summary>
    public double? Latitude { get; set; }

    /// <summary>Obavezno osim za čvorove i mjesta ukrcavanja (LocationType 3 i 4).</summary>
    public double? Longitude { get; set; }

    /// <summary>Tarifna zona (GTFS zone_id).</summary>
    public string? ZoneId { get; set; }

    public string? Url { get; set; }

    /// <summary>0 stajalište/peron, 1 kolodvor, 2 ulaz, 3 čvor, 4 mjesto ukrcavanja.</summary>
    public LocationType LocationType { get; set; } = LocationType.StopOrPlatform;

    public Guid? ParentStationId { get; set; }

    /// <summary>IANA zona, samo ako se razlikuje od zone prijevoznika.</summary>
    public string? Timezone { get; set; }

    public string? PlatformCode { get; set; }
    public WheelchairBoarding WheelchairBoarding { get; set; } = WheelchairBoarding.Unknown;
    public bool IsActive { get; set; } = true;
}
