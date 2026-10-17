using System.Text.Json.Serialization;

namespace Terminus.Model.Common.Enums;

/// <summary>Odakle je varijanta linije.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<VariantSource>))]
public enum VariantSource : short
{
    /// <summary>Napravio ju je korisnik.</summary>
    Manual = 0,

    /// <summary>Prepoznata iz vožnji (GTFS uvoz).</summary>
    Gtfs = 1
}
