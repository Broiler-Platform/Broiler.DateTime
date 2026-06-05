using Broiler.DateTime;
using Xunit;

namespace Broiler.DateTime.Tests;

public class InteropTests
{
    [Fact]
    public void ToDateTimeOffset_round_trips_in_range()
    {
        var v = ExtendedIsoDateTime.Parse("2025-06-05T14:30:00.1234567+02:00");
        DateTimeOffset dto = v.ToDateTimeOffset();
        Assert.Equal(2025, dto.Year);
        Assert.Equal(6, dto.Month);
        Assert.Equal(5, dto.Day);
        Assert.Equal(14, dto.Hour);
        Assert.Equal(TimeSpan.FromHours(2), dto.Offset);
        Assert.Equal(1234567, dto.Ticks % TimeSpan.TicksPerSecond);
    }

    [Fact]
    public void ToDateTimeOffset_throws_without_offset()
    {
        var v = ExtendedIsoDateTime.Parse("2025-06-05T14:30:00");
        Assert.Throws<InvalidOperationException>(() => v.ToDateTimeOffset());
    }

    [Fact]
    public void Expanded_year_is_outside_DateTimeOffset_range()
    {
        var v = ExtendedIsoDateTime.Parse("+010000-01-01T00:00:00Z");
        Assert.False(v.TryToDateTimeOffset(out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => v.ToDateTimeOffset());
    }

    [Fact]
    public void Negative_year_is_outside_DateTimeOffset_range()
    {
        var v = ExtendedIsoDateTime.Parse("-000001-01-01T00:00:00Z");
        Assert.False(v.TryToDateTimeOffset(out _));
    }

    [Fact]
    public void FromDateTimeOffset_preserves_components()
    {
        var dto = new DateTimeOffset(2025, 6, 5, 14, 30, 0, TimeSpan.FromHours(-5));
        var v = ExtendedIsoDateTime.FromDateTimeOffset(dto);
        Assert.Equal("2025-06-05T14:30:00-05:00", v.ToStringIso());
    }

    [Fact]
    public void FromDateTime_utc_becomes_Z()
    {
        var dt = new System.DateTime(2025, 6, 5, 14, 30, 0, DateTimeKind.Utc);
        var v = ExtendedIsoDateTime.FromDateTime(dt);
        Assert.Equal(TimeSpan.Zero, v.Offset);
        Assert.Equal("2025-06-05T14:30:00Z", v.ToStringIso());
    }

    [Fact]
    public void FromDateTime_unspecified_has_no_offset()
    {
        var dt = new System.DateTime(2025, 6, 5, 14, 30, 0, DateTimeKind.Unspecified);
        var v = ExtendedIsoDateTime.FromDateTime(dt);
        Assert.False(v.HasOffset);
    }

    [Fact]
    public void DateTimeOffset_round_trip()
    {
        var original = new DateTimeOffset(2025, 6, 5, 14, 30, 45, 123, TimeSpan.FromHours(3));
        var v = ExtendedIsoDateTime.FromDateTimeOffset(original);
        DateTimeOffset back = v.ToDateTimeOffset();
        Assert.Equal(original, back);
    }
}
