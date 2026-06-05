using Broiler.DateTime;
using Xunit;

namespace Broiler.DateTime.Tests;

public class ParsingTests
{
    [Fact]
    public void Parses_basic_utc()
    {
        var v = ExtendedIsoDateTime.Parse("2025-06-05T14:30:00Z");
        Assert.Equal(2025, v.Year);
        Assert.Equal(6, v.Month);
        Assert.Equal(5, v.Day);
        Assert.Equal(14, v.Hour);
        Assert.Equal(30, v.Minute);
        Assert.Equal(0, v.Second);
        Assert.Equal(0, v.Nanosecond);
        Assert.Equal(TimeSpan.Zero, v.Offset);
    }

    [Fact]
    public void Parses_milliseconds()
    {
        var v = ExtendedIsoDateTime.Parse("2025-06-05T14:30:00.123Z");
        Assert.Equal(123_000_000, v.Nanosecond);
    }

    [Fact]
    public void Parses_nanoseconds()
    {
        var v = ExtendedIsoDateTime.Parse("2025-06-05T14:30:00.123456789Z");
        Assert.Equal(123_456_789, v.Nanosecond);
    }

    [Fact]
    public void Parses_positive_offset()
    {
        var v = ExtendedIsoDateTime.Parse("2025-06-05T14:30:00+02:00");
        Assert.Equal(TimeSpan.FromHours(2), v.Offset);
    }

    [Fact]
    public void Parses_negative_offset()
    {
        var v = ExtendedIsoDateTime.Parse("2025-06-05T14:30:00-05:30");
        Assert.Equal(new TimeSpan(-5, -30, 0), v.Offset);
    }

    [Fact]
    public void Parses_unspecified_offset()
    {
        var v = ExtendedIsoDateTime.Parse("2025-06-05T14:30:00");
        Assert.False(v.HasOffset);
        Assert.Null(v.Offset);
    }

    [Fact]
    public void Parses_expanded_positive_year()
    {
        var v = ExtendedIsoDateTime.Parse("+010000-01-01T00:00:00Z");
        Assert.Equal(10000, v.Year);
        Assert.Equal(1, v.Month);
        Assert.Equal(1, v.Day);
    }

    [Fact]
    public void Parses_negative_year()
    {
        var v = ExtendedIsoDateTime.Parse("-000001-01-01T00:00:00Z");
        Assert.Equal(-1, v.Year);
    }

    [Fact]
    public void Parses_year_zero()
    {
        var v = ExtendedIsoDateTime.Parse("0000-01-01T00:00:00Z");
        Assert.Equal(0, v.Year);
    }

    [Fact]
    public void Parses_lowercase_t_and_z()
    {
        var v = ExtendedIsoDateTime.Parse("2025-06-05t14:30:00z");
        Assert.Equal(TimeSpan.Zero, v.Offset);
        Assert.Equal(14, v.Hour);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-date")]
    [InlineData("2025-13-01T00:00:00Z")]   // bad month
    [InlineData("2025-00-01T00:00:00Z")]   // month 0
    [InlineData("2025-06-31T00:00:00Z")]   // June has 30 days
    [InlineData("2025-02-29T00:00:00Z")]   // 2025 not a leap year
    [InlineData("2025-06-05T24:00:00Z")]   // hour out of range
    [InlineData("2025-06-05T14:60:00Z")]   // minute out of range
    [InlineData("2025-06-05T14:30:60Z")]   // leap second not supported
    [InlineData("2025-6-5T14:30:00Z")]     // unpadded fields
    [InlineData("25-06-05T14:30:00Z")]     // 2-digit year without sign
    [InlineData("2025-06-05T14:30:00.Z")]  // empty fraction
    [InlineData("2025-06-05T14:30:00+2:00")] // unpadded offset
    [InlineData("2025-06-05T14:30:00 extra")] // trailing junk
    public void TryParse_returns_false_on_invalid(string text)
    {
        Assert.False(ExtendedIsoDateTime.TryParse(text, out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Parse_throws_on_invalid()
    {
        Assert.Throws<FormatException>(() => ExtendedIsoDateTime.Parse("nope"));
    }
}
