namespace Terminus.Common;

/// <summary>
/// Pretvorba "dan voznog reda + GTFS vrijeme" u stvarni trenutak.
/// </summary>
/// <remarks>
/// GTFS vremena ne računaju se od ponoći nego od "podneva minus 12 h" lokalnog
/// vremena. Na običan dan to je ponoć, a na dan prelaska na ljetno ili zimsko
/// računanje vremena sat ranije ili kasnije. Zato je npr. 06:00:00 i tada
/// stvarno 06:00 na satu. Vremena iznad 24 h padaju na sljedeći kalendarski dan.
/// </remarks>
public static class ServiceDay
{
    /// <summary>Početak dana voznog reda: lokalno podne umanjeno za 12 h.</summary>
    public static DateTimeOffset Start(DateOnly serviceDate, TimeZoneInfo timeZone)
    {
        // U podne nema prijelaza na ljetno/zimsko vrijeme, pa je pomak jednoznačan.
        var noon = serviceDate.ToDateTime(new TimeOnly(12, 0));
        return new DateTimeOffset(noon, timeZone.GetUtcOffset(noon)).AddHours(-12);
    }

    /// <summary>Stvarni trenutak GTFS vremena (sekunde) u danu voznog reda, u zadanoj zoni.</summary>
    public static DateTimeOffset ToDateTimeOffset(DateOnly serviceDate, int seconds, TimeZoneInfo timeZone) =>
        TimeZoneInfo.ConvertTime(Start(serviceDate, timeZone).AddSeconds(seconds), timeZone);
}
