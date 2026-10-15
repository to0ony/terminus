using Terminus.Common;

namespace Terminus.Tests.Common;

public class GtfsTimeTests
{
    [Theory]
    [InlineData("06:15:00", 6 * 3600 + 15 * 60)]
    [InlineData("6:15:00", 6 * 3600 + 15 * 60)]
    [InlineData("06:15", 6 * 3600 + 15 * 60)]
    [InlineData(" 07:05:30 ", 7 * 3600 + 5 * 60 + 30)]
    [InlineData("00:00:00", 0)]
    [InlineData("25:30:00", 91800)]
    [InlineData("48:00:00", GtfsTime.MaxSeconds)]
    public void TryParse_ValidValue_ReturnsSeconds(string value, int expected)
    {
        Assert.True(GtfsTime.TryParse(value, out var seconds));
        Assert.Equal(expected, seconds);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("6")]
    [InlineData("06:5")]
    [InlineData("06:60:00")]
    [InlineData("06:15:60")]
    [InlineData("06:15:00:00")]
    [InlineData("100:00:00")]
    [InlineData("-1:00:00")]
    [InlineData("06:1a:00")]
    public void TryParse_InvalidValue_ReturnsFalse(string? value)
    {
        Assert.False(GtfsTime.TryParse(value, out _));
    }

    [Theory]
    [InlineData(0, "00:00:00")]
    [InlineData(6 * 3600 + 15 * 60, "06:15:00")]
    [InlineData(91800, "25:30:00")]
    [InlineData(GtfsTime.MaxSeconds, "48:00:00")]
    public void Format_Seconds_ReturnsGtfsTime(int seconds, string expected)
    {
        Assert.Equal(expected, GtfsTime.Format(seconds));
    }

    [Theory]
    [InlineData("06:15:00")]
    [InlineData("23:59:59")]
    [InlineData("25:30:00")]
    public void Format_AfterTryParse_ReturnsOriginalValue(string value)
    {
        Assert.True(GtfsTime.TryParse(value, out var seconds));
        Assert.Equal(value, GtfsTime.Format(seconds));
    }
}
