using Terminus.Model.Common.Enums;

namespace Terminus.Model.Common;

public interface IStopModel
{
    Guid Id { get; set; }

    /// <summary>GTFS stop_id, jedinstven.</summary>
    string Code { get; set; }

    /// <summary>Oznaka stanice za putnike (GTFS stop_code), npr. broj na stupu.</summary>
    string? PublicCode { get; set; }

    string Name { get; set; }

    /// <summary>Naziv za čitače zaslona i govornu sintezu (GTFS tts_stop_name).</summary>
    string? TtsName { get; set; }

    string? Description { get; set; }

    /// <summary>Obavezno osim za čvorove i mjesta ukrcavanja (location_type 3 i 4).</summary>
    double? Latitude { get; set; }

    /// <summary>Obavezno osim za čvorove i mjesta ukrcavanja (location_type 3 i 4).</summary>
    double? Longitude { get; set; }

    /// <summary>Tarifna zona (GTFS zone_id), za pravila cijena karata.</summary>
    string? ZoneId { get; set; }

    /// <summary>Web stranica stanice (GTFS stop_url).</summary>
    string? Url { get; set; }

    LocationType LocationType { get; set; }
    Guid? ParentStationId { get; set; }

    /// <summary>IANA vremenska zona stanice ako se razlikuje od zone prijevoznika (GTFS stop_timezone).</summary>
    string? Timezone { get; set; }

    string? PlatformCode { get; set; }
    WheelchairBoarding WheelchairBoarding { get; set; }
    bool IsActive { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
