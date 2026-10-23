using System.Text.Json.Serialization;

namespace Terminus.Model.Common.Enums;

/// <summary>Kako linija zadaje vremena na stanicama.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TimeMode>))]
public enum TimeMode : short
{
    /// <summary>Točna vremena na svim stanicama (iz varijante, dotjeruju se po vožnji).</summary>
    Exact = 0,

    /// <summary>
    /// Zadaju se polazak i dolazak na terminus; vremena međustanica su okvirna
    /// (GTFS timepoint = 0), raspoređena razmjerno vremenima vožnje.
    /// </summary>
    Approximate = 1
}
