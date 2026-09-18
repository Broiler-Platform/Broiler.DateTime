using Broiler.DateTime;
using Xunit;

namespace Broiler.DateTime.Tests;

public class ArithmeticTests
{
    [Fact]
    public void AddDays_crosses_month_boundary()
    {
        var v = new ExtendedIsoDateTime(2025, 1, 31).AddDays(1);
        Assert.Equal((2025L, 2, 1), (v.Year, v.Month, v.Day));
    }

    [Fact]
    public void AddDays_crosses_year_boundary()
    {
        var v = new ExtendedIsoDateTime(2025, 12, 31).AddDays(1);
        Assert.Equal((2026L, 1, 1), (v.Year, v.Month, v.Day));
    }

    [Fact]
    public void AddDays_negative_crosses_year_boundary_backwards()
    {
        var v = new ExtendedIsoDateTime(2026, 1, 1).AddDays(-1);
        Assert.Equal((2025L, 12, 31), (v.Year, v.Month, v.Day));
    }

    [Fact]
    public void AddDays_crosses_year_zero()
    {
        var v = new ExtendedIsoDateTime(0, 1, 1).AddDays(-1);
        Assert.Equal((-1L, 12, 31), (v.Year, v.Month, v.Day));
    }

    [Fact]
    public void AddDays_preserves_time_and_offset()
    {
        var v = new ExtendedIsoDateTime(2025, 1, 31, 14, 30, 15, 500, TimeSpan.FromHours(2)).AddDays(1);
        Assert.Equal(14, v.Hour);
        Assert.Equal(30, v.Minute);
        Assert.Equal(15, v.Second);
        Assert.Equal(500, v.Nanosecond);
        Assert.Equal(TimeSpan.FromHours(2), v.Offset);
    }

    [Fact]
    public void AddMonths_clamps_end_of_month()
    {
        var v = new ExtendedIsoDateTime(2025, 1, 31).AddMonths(1);
        Assert.Equal((2025L, 2, 28), (v.Year, v.Month, v.Day));
    }

    [Fact]
    public void AddMonths_clamps_to_leap_day()
    {
        var v = new ExtendedIsoDateTime(2024, 1, 31).AddMonths(1);
        Assert.Equal((2024L, 2, 29), (v.Year, v.Month, v.Day));
    }

    [Fact]
    public void AddMonths_rolls_over_year()
    {
        var v = new ExtendedIsoDateTime(2025, 11, 15).AddMonths(3);
        Assert.Equal((2026L, 2, 15), (v.Year, v.Month, v.Day));
    }

    [Fact]
    public void AddMonths_negative_rolls_back_year()
    {
        var v = new ExtendedIsoDateTime(2025, 1, 15).AddMonths(-2);
        Assert.Equal((2024L, 11, 15), (v.Year, v.Month, v.Day));
    }

    [Fact]
    public void AddYears_clamps_leap_day()
    {
        var v = new ExtendedIsoDateTime(2024, 2, 29).AddYears(1);
        Assert.Equal((2025L, 2, 28), (v.Year, v.Month, v.Day));
    }

    [Fact]
    public void AddYears_keeps_leap_day_on_leap_target()
    {
        var v = new ExtendedIsoDateTime(2024, 2, 29).AddYears(4);
        Assert.Equal((2028L, 2, 29), (v.Year, v.Month, v.Day));
    }

    [Fact]
    public void Difference_in_hours()
    {
        var a = ExtendedIsoDateTime.Parse("2025-06-05T14:00:00Z");
        var b = ExtendedIsoDateTime.Parse("2025-06-05T12:00:00Z");
        Assert.Equal(TimeSpan.FromHours(2), a.Difference(b));
    }

    [Fact]
    public void Difference_accounts_for_offset()
    {
        // Same instant: 12:00Z == 14:00+02:00
        var a = ExtendedIsoDateTime.Parse("2025-06-05T12:00:00Z");
        var b = ExtendedIsoDateTime.Parse("2025-06-05T14:00:00+02:00");
        Assert.Equal(TimeSpan.Zero, a.Difference(b));
    }

    [Fact]
    public void Difference_supports_spans_greater_than_292_years()
    {
        // 500 years difference previously overflowed long when converting days to nanoseconds.
        var a = ExtendedIsoDateTime.Parse("2500-01-01T00:00:00Z");
        var b = ExtendedIsoDateTime.Parse("2000-01-01T00:00:00Z");
        TimeSpan diff = a.Difference(b);
        Assert.True(diff > TimeSpan.Zero);
        Assert.Equal(a.DaysBetween(b), (long)diff.TotalDays);

        // Reverse difference is symmetric
        TimeSpan reverse = b.Difference(a);
        Assert.Equal(-diff, reverse);
    }

    [Fact]
    public void Difference_supports_large_span_within_timespan_limits()
    {
        // 20,000 years fits comfortably within TimeSpan (~29,227 years).
        var a = ExtendedIsoDateTime.Parse("+020000-01-01T00:00:00Z");
        var b = ExtendedIsoDateTime.Parse("0000-01-01T00:00:00Z");
        TimeSpan diff = a.Difference(b);
        Assert.True(diff > TimeSpan.Zero);
        Assert.Equal(a.DaysBetween(b), (long)diff.TotalDays);
    }

    [Fact]
    public void Difference_throws_when_exceeding_timespan_limits()
    {
        // 50,000 years exceeds TimeSpan.MaxValue (~29,227 years).
        var a = ExtendedIsoDateTime.Parse("+050000-01-01T00:00:00Z");
        var b = ExtendedIsoDateTime.Parse("0000-01-01T00:00:00Z");
        Assert.Throws<OverflowException>(() => a.Difference(b));
    }

    [Fact]
    public void Difference_sub_tick_fractional_nanoseconds_truncated()
    {
        var a = new ExtendedIsoDateTime(2025, 1, 1, 0, 0, 0, 150, TimeSpan.Zero);
        var b = new ExtendedIsoDateTime(2025, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);
        // 150 ns = 1 tick (100 ns) + 50 ns (truncated)
        Assert.Equal(1, a.Difference(b).Ticks);
        Assert.Equal(-1, b.Difference(a).Ticks);
    }

    [Fact]
    public void DaysBetween_counts_calendar_days()
    {
        var a = new ExtendedIsoDateTime(2025, 3, 1);
        var b = new ExtendedIsoDateTime(2025, 1, 1);
        Assert.Equal(59, a.DaysBetween(b)); // Jan(31) + Feb(28)
    }
}

