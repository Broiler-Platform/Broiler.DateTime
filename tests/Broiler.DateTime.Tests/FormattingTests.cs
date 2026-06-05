using Broiler.DateTime;
using Xunit;

namespace Broiler.DateTime.Tests;

public class FormattingTests
{
    [Theory]
    [InlineData("2025-06-05T14:30:00Z")]
    [InlineData("2025-06-05T14:30:00.123Z")]
    [InlineData("2025-06-05T14:30:00.123456789Z")]
    [InlineData("2025-06-05T14:30:00+02:00")]
    [InlineData("2025-06-05T14:30:00-05:30")]
    [InlineData("2025-06-05T14:30:00")]
    [InlineData("+010000-01-01T00:00:00Z")]
    [InlineData("-000001-01-01T00:00:00Z")]
    [InlineData("0000-01-01T00:00:00Z")]
    public void Round_trips_through_string(string text)
    {
        var v = ExtendedIsoDateTime.Parse(text);
        Assert.Equal(text, v.ToStringIso());
        Assert.Equal(text, v.ToString());
    }

    [Fact]
    public void Zero_offset_formats_as_Z()
    {
        var v = ExtendedIsoDateTime.Parse("2025-06-05T14:30:00+00:00");
        Assert.Equal("2025-06-05T14:30:00Z", v.ToStringIso());
    }

    [Fact]
    public void Trailing_zeros_in_fraction_are_trimmed()
    {
        var v = ExtendedIsoDateTime.Parse("2025-06-05T14:30:00.100000000Z");
        Assert.Equal("2025-06-05T14:30:00.1Z", v.ToStringIso());
    }

    [Fact]
    public void Large_year_uses_expanded_form()
    {
        var v = new ExtendedIsoDateTime(123456, 1, 1, offset: TimeSpan.Zero);
        Assert.Equal("+123456-01-01T00:00:00Z", v.ToStringIso());
    }

    [Fact]
    public void Year_9999_uses_four_digits()
    {
        var v = new ExtendedIsoDateTime(9999, 12, 31, offset: TimeSpan.Zero);
        Assert.Equal("9999-12-31T00:00:00Z", v.ToStringIso());
    }

    [Fact]
    public void Year_10000_uses_expanded_six_digits()
    {
        var v = new ExtendedIsoDateTime(10000, 1, 1, offset: TimeSpan.Zero);
        Assert.Equal("+010000-01-01T00:00:00Z", v.ToStringIso());
    }

    [Fact]
    public void Negative_year_pads_to_six_digits()
    {
        var v = new ExtendedIsoDateTime(-1, 1, 1, offset: TimeSpan.Zero);
        Assert.Equal("-000001-01-01T00:00:00Z", v.ToStringIso());
    }

    [Fact]
    public void Javascript_toISOString_compatible()
    {
        // Date.toISOString() always emits 3-digit milliseconds in UTC; we should parse it
        // and re-emit an equivalent (trimmed) form.
        var v = ExtendedIsoDateTime.Parse("2025-06-05T14:30:00.000Z");
        Assert.Equal("2025-06-05T14:30:00Z", v.ToStringIso());

        var v2 = ExtendedIsoDateTime.Parse("2025-06-05T14:30:00.250Z");
        Assert.Equal("2025-06-05T14:30:00.25Z", v2.ToStringIso());
    }
}
