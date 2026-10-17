using System.Text.Json.Serialization;

namespace Terminus.Model.Common.Enums;

/// <summary>Odakle je iznimka dana sezone.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<SeasonDaySource>))]
public enum SeasonDaySource : short
{
    /// <summary>Unio ju je korisnik; ostaje i kad se promijeni pravilo za praznike.</summary>
    Manual = 0,

    /// <summary>Praznik iz popisa praznika države; mijenja se s pravilom za praznike.</summary>
    Holiday = 1
}
