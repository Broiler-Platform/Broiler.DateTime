using Broiler.DateTime;
using Xunit;

namespace Broiler.DateTime.Tests;

public class CalendarTests
{
    [Theory]
    [InlineData(2000, true)]   // divisible by 400
    [InlineData(1900, false)]  // divisible by 100 but not 400
    [InlineData(2024, true)]   // divisible by 4
    [InlineData(2025, false)]  // not divisible by 4
    [InlineData(0, true)]      // year 0 is divisible by 400
    [InlineData(-1, false)]    // 2 BCE: -1 not divisible by 4
    [InlineData(-4, true)]     // 5 BCE: -4 divisible by 4, not 100
    [InlineData(-400, true)]   // divisible by 400
    [InlineData(-100, false)]  // divisible by 100 not 400
    public void IsLeapYear_follows_gregorian_rules(long year, bool expected)
    {
        Assert.Equal(expected, ExtendedIsoDateTime.IsLeapYear(year));
    }

    [Fact]
    public void Feb29_valid_in_leap_year()
    {
        var v = new ExtendedIsoDateTime(2024, 2, 29);
        Assert.Equal(29, v.Day);
    }

    [Fact]
    public void Feb29_invalid_in_common_year()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExtendedIsoDateTime(2025, 2, 29));
    }

    [Theory]
    [InlineData(2025, 1, 31)]
    [InlineData(2025, 4, 30)]
    [InlineData(2024, 2, 29)]
    [InlineData(2025, 2, 28)]
    [InlineData(2025, 12, 31)]
    public void DaysInMonth_is_correct(long year, int month, int expected)
    {
        Assert.Equal(expected, ExtendedIsoDateTime.DaysInMonth(year, month));
    }

    [Fact]
    public void Day_zero_is_invalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExtendedIsoDateTime(2025, 1, 0));
    }

    [Theory]
    // Round-trip the civil <-> day-number algorithms across the year boundaries we care about.
    [InlineData(1970, 1, 1)]
    [InlineData(2000, 2, 29)]
    [InlineData(1, 1, 1)]
    [InlineData(0, 1, 1)]
    [InlineData(-1, 12, 31)]
    [InlineData(10000, 1, 1)]
    [InlineData(-123456, 7, 15)]
    public void DayNumber_round_trips(long year, int month, int day)
    {
        long dn = ExtendedIsoDateTime.DaysFromCivil(year, month, day);
        (long y, int m, int d) = ExtendedIsoDateTime.CivilFromDays(dn);
        Assert.Equal((year, month, day), (y, m, d));
    }

    [Fact]
    public void Year_minus_one_precedes_year_zero()
    {
        // -1-12-31 is exactly one day before 0000-01-01.
        long a = ExtendedIsoDateTime.DaysFromCivil(-1, 12, 31);
        long b = ExtendedIsoDateTime.DaysFromCivil(0, 1, 1);
        Assert.Equal(1, b - a);
    }
}
