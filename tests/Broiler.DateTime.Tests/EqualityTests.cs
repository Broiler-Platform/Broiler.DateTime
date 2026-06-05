using Broiler.DateTime;
using Xunit;

namespace Broiler.DateTime.Tests;

public class EqualityTests
{
    [Fact]
    public void Same_instant_different_offset_are_equal()
    {
        var a = ExtendedIsoDateTime.Parse("2025-06-05T12:00:00Z");
        var b = ExtendedIsoDateTime.Parse("2025-06-05T14:00:00+02:00");
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Same_instant_different_offset_not_exactly_equal()
    {
        var a = ExtendedIsoDateTime.Parse("2025-06-05T12:00:00Z");
        var b = ExtendedIsoDateTime.Parse("2025-06-05T14:00:00+02:00");
        Assert.False(a.EqualsExact(b));
    }

    [Fact]
    public void Z_and_plus_zero_are_exactly_equal()
    {
        var a = ExtendedIsoDateTime.Parse("2025-06-05T12:00:00Z");
        var b = ExtendedIsoDateTime.Parse("2025-06-05T12:00:00+00:00");
        Assert.True(a.EqualsExact(b));
    }

    [Fact]
    public void Comparison_orders_by_instant()
    {
        var earlier = ExtendedIsoDateTime.Parse("2025-06-05T12:00:00Z");
        var later = ExtendedIsoDateTime.Parse("2025-06-05T13:00:00Z");
        Assert.True(earlier < later);
        Assert.True(later > earlier);
        Assert.True(earlier <= later);
    }

    [Fact]
    public void Comparison_spans_year_zero()
    {
        var bce = ExtendedIsoDateTime.Parse("-000001-12-31T00:00:00Z");
        var yearZero = ExtendedIsoDateTime.Parse("0000-01-01T00:00:00Z");
        Assert.True(bce < yearZero);
    }

    [Fact]
    public void Sort_uses_instant_order()
    {
        var items = new[]
        {
            ExtendedIsoDateTime.Parse("2025-06-05T13:00:00Z"),
            ExtendedIsoDateTime.Parse("2025-06-05T11:00:00Z"),
            ExtendedIsoDateTime.Parse("2025-06-05T12:00:00Z"),
        };
        Array.Sort(items);
        Assert.Equal(11, items[0].Hour);
        Assert.Equal(12, items[1].Hour);
        Assert.Equal(13, items[2].Hour);
    }

    [Fact]
    public void Deconstruct_date_only()
    {
        var (y, m, d) = ExtendedIsoDateTime.Parse("+010000-03-15T00:00:00Z");
        Assert.Equal((10000L, 3, 15), (y, m, d));
    }

    [Fact]
    public void Deconstruct_full()
    {
        var (y, mo, d, h, mi, s, ns, off) =
            ExtendedIsoDateTime.Parse("2025-06-05T14:30:45.5+02:00");
        Assert.Equal(2025L, y);
        Assert.Equal(6, mo);
        Assert.Equal(5, d);
        Assert.Equal(14, h);
        Assert.Equal(30, mi);
        Assert.Equal(45, s);
        Assert.Equal(500_000_000, ns);
        Assert.Equal(TimeSpan.FromHours(2), off);
    }
}
