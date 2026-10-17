using System.Text.Json.Serialization;

namespace Terminus.Model.Common.Enums;

/// <summary>Kako vozi dan sezone (i pravilo za praznike).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<SeasonDayMode>))]
public enum SeasonDayMode : short
{
    /// <summary>Kao inače za taj dan u tjednu.</summary>
    Regular = 0,

    /// <summary>Vozi kao zadani tip dana (npr. praznik kao nedjelja).</summary>
    RunsAs = 1,

    /// <summary>Ne vozi ništa.</summary>
    NoService = 2
}
