using Terminus.Common;

namespace Terminus.Tests.Common;

public class ServiceDayTests
{
    private static readonly TimeZoneInfo Zagreb = TimeZoneInfo.FindSystemTimeZoneById("Europe/Zagreb");

    [Fact]
    public void Start_OrdinaryDay_IsLocalMidnight()
    {
        var start = ServiceDay.Start(new DateOnly(2026, 12, 23), Zagreb);

        AssertSame(new DateTimeOffset(2026, 12, 23, 0, 0, 0, TimeSpan.FromHours(1)), start);
    }

    [Fact]
    public void ToDateTimeOffset_OrdinaryDay_ReturnsLocalTime()
    {
        var departure = ServiceDay.ToDateTimeOffset(new DateOnly(2026, 12, 23), Seconds("07:05:00"), Zagreb);

        AssertSame(new DateTimeOffset(2026, 12, 23, 7, 5, 0, TimeSpan.FromHours(1)), departure);
    }

    [Fact]
    public void ToDateTimeOffset_AfterMidnight_FallsOnNextDay()
    {
        // Vožnja voznog reda za srijedu u 24:20 stvarno kreće u četvrtak u 00:20.
        var departure = ServiceDay.ToDateTimeOffset(new DateOnly(2026, 12, 23), Seconds("24:20:00"), Zagreb);

        AssertSame(new DateTimeOffset(2026, 12, 24, 0, 20, 0, TimeSpan.FromHours(1)), departure);
    }

    [Fact]
    public void ToDateTimeOffset_MaxTime_FallsTwoDaysLater()
    {
        var departure = ServiceDay.ToDateTimeOffset(new DateOnly(2026, 12, 23), GtfsTime.MaxSeconds, Zagreb);

        AssertSame(new DateTimeOffset(2026, 12, 25, 0, 0, 0, TimeSpan.FromHours(1)), departure);
    }

    [Fact]
    public void ToDateTimeOffset_SpringForwardDay_KeepsWallClockTime()
    {
        // 29.3.2026. u 02:00 sat se pomiče na 03:00 (CET → CEST).
        var departure = ServiceDay.ToDateTimeOffset(new DateOnly(2026, 3, 29), Seconds("06:00:00"), Zagreb);

        AssertSame(new DateTimeOffset(2026, 3, 29, 6, 0, 0, TimeSpan.FromHours(2)), departure);
    }

    [Fact]
    public void ToDateTimeOffset_FallBackDay_KeepsWallClockTime()
    {
        // 25.10.2026. u 03:00 sat se vraća na 02:00 (CEST → CET).
        var departure = ServiceDay.ToDateTimeOffset(new DateOnly(2026, 10, 25), Seconds("06:00:00"), Zagreb);

        AssertSame(new DateTimeOffset(2026, 10, 25, 6, 0, 0, TimeSpan.FromHours(1)), departure);
    }

    [Fact]
    public void Start_SpringForwardDay_IsOneHourBeforeMidnight()
    {
        // Pravilo "podne minus 12 h": dan od 23 sata počinje sat prije ponoći.
        var start = ServiceDay.Start(new DateOnly(2026, 3, 29), Zagreb);

        Assert.Equal(new DateTimeOffset(2026, 3, 28, 22, 0, 0, TimeSpan.Zero), start);
    }

    [Fact]
    public void Start_FallBackDay_IsOneHourAfterMidnight()
    {
        // Dan od 25 sati počinje sat nakon ponoći.
        var start = ServiceDay.Start(new DateOnly(2026, 10, 25), Zagreb);

        Assert.Equal(new DateTimeOffset(2026, 10, 24, 23, 0, 0, TimeSpan.Zero), start);
    }

    private static int Seconds(string gtfsTime) =>
        GtfsTime.TryParse(gtfsTime, out var seconds) ? seconds : throw new ArgumentException(gtfsTime);

    /// <summary>Isti trenutak i isti pomak od UTC-a (Assert.Equal za DateTimeOffset gleda samo trenutak).</summary>
    private static void AssertSame(DateTimeOffset expected, DateTimeOffset actual)
    {
        Assert.Equal(expected, actual);
        Assert.Equal(expected.Offset, actual.Offset);
    }
}
