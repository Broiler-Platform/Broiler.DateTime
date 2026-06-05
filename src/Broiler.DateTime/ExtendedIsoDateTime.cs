using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;

namespace Broiler.DateTime;

/// <summary>
/// An immutable ISO-8601 / RFC-3339-like date-time value whose year range is not limited
/// to the .NET <see cref="System.DateTime"/> range. It supports ISO expanded years such as
/// <c>+010000</c> as well as negative / astronomical years such as <c>-000001</c>.
/// </summary>
/// <remarks>
/// <para><b>Calendar.</b> The proleptic Gregorian calendar is used for all years (the Gregorian
/// rules are projected backwards before their historical introduction).</para>
///
/// <para><b>Year numbering.</b> <i>Astronomical</i> year numbering is used, which means:</para>
/// <list type="bullet">
///   <item><description>Year <c>0</c> exists and is a valid year.</description></item>
///   <item><description>Year <c>0</c> corresponds to 1 BCE, year <c>-1</c> to 2 BCE, and so on.</description></item>
/// </list>
///
/// <para><b>Leap years.</b> A year is a leap year when it is divisible by 4, except years
/// divisible by 100 which are not, except years divisible by 400 which are. This rule is applied
/// to all years including <c>0</c> and negative years.</para>
///
/// <para><b>Time zones.</b> Only fixed UTC offsets are supported. There is no IANA / Olson
/// time-zone database. A value may also have an <i>unspecified</i> offset (no offset information).</para>
///
/// <para><b>Precision.</b> Fractional seconds are stored with nanosecond precision.</para>
/// </remarks>
[JsonConverter(typeof(ExtendedIsoDateTimeJsonConverter))]
public sealed class ExtendedIsoDateTime
    : IEquatable<ExtendedIsoDateTime>, IComparable<ExtendedIsoDateTime>, IComparable
{
    /// <summary>Number of nanoseconds in a single second.</summary>
    public const long NanosecondsPerSecond = 1_000_000_000L;

    private const long NanosecondsPerDay = 86_400L * NanosecondsPerSecond;
    private const long TicksPerNanosecondDivisor = 100L; // 1 tick == 100 ns

    private static readonly int[] DaysInMonthCommon = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

    // Day number of 0001-01-01 in the "days since 1970-01-01" system used by the civil algorithms.
    private static readonly long DayNumberOfYear0001 = DaysFromCivil(1, 1, 1);

    /// <summary>The signed year using astronomical numbering (year 0 exists; -1 == 2 BCE).</summary>
    public long Year { get; }

    /// <summary>The month of the year, from 1 (January) to 12 (December).</summary>
    public int Month { get; }

    /// <summary>The day of the month, from 1 to the number of days in <see cref="Month"/>.</summary>
    public int Day { get; }

    /// <summary>The hour of the day, from 0 to 23.</summary>
    public int Hour { get; }

    /// <summary>The minute of the hour, from 0 to 59.</summary>
    public int Minute { get; }

    /// <summary>The second of the minute, from 0 to 59. Leap seconds (60) are not supported.</summary>
    public int Second { get; }

    /// <summary>The fractional part of the second expressed in nanoseconds, from 0 to 999,999,999.</summary>
    public int Nanosecond { get; }

    /// <summary>
    /// The fixed UTC offset, or <see langword="null"/> when the offset is unspecified
    /// (a "local"/offset-less value). When present the offset must be a whole number of minutes.
    /// </summary>
    public TimeSpan? Offset { get; }

    /// <summary>Gets a value indicating whether this value carries a fixed UTC offset.</summary>
    public bool HasOffset => Offset.HasValue;

    /// <summary>
    /// Creates a new <see cref="ExtendedIsoDateTime"/> and validates every component.
    /// </summary>
    /// <param name="year">Signed year using astronomical numbering (year 0 is valid).</param>
    /// <param name="month">Month, 1 to 12.</param>
    /// <param name="day">Day of month, validated against <paramref name="month"/> and the leap-year rule.</param>
    /// <param name="hour">Hour, 0 to 23.</param>
    /// <param name="minute">Minute, 0 to 59.</param>
    /// <param name="second">Second, 0 to 59.</param>
    /// <param name="nanosecond">Fractional second in nanoseconds, 0 to 999,999,999.</param>
    /// <param name="offset">Fixed UTC offset, or <see langword="null"/> for an unspecified offset.</param>
    /// <exception cref="ArgumentOutOfRangeException">A component is outside its valid range.</exception>
    public ExtendedIsoDateTime(
        long year,
        int month,
        int day,
        int hour = 0,
        int minute = 0,
        int second = 0,
        int nanosecond = 0,
        TimeSpan? offset = null)
    {
        if (month is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(month), month, "Month must be between 1 and 12.");

        int maxDay = DaysInMonth(year, month);
        if (day < 1 || day > maxDay)
            throw new ArgumentOutOfRangeException(nameof(day), day,
                $"Day must be between 1 and {maxDay} for {year:+0000;-0000;0000}-{month:D2}.");

        if (hour is < 0 or > 23)
            throw new ArgumentOutOfRangeException(nameof(hour), hour, "Hour must be between 0 and 23.");
        if (minute is < 0 or > 59)
            throw new ArgumentOutOfRangeException(nameof(minute), minute, "Minute must be between 0 and 59.");
        if (second is < 0 or > 59)
            throw new ArgumentOutOfRangeException(nameof(second), second,
                "Second must be between 0 and 59 (leap seconds are not supported).");
        if (nanosecond is < 0 or > 999_999_999)
            throw new ArgumentOutOfRangeException(nameof(nanosecond), nanosecond,
                "Nanosecond must be between 0 and 999,999,999.");

        if (offset is TimeSpan o)
        {
            if (o.Ticks % TimeSpan.TicksPerMinute != 0)
                throw new ArgumentOutOfRangeException(nameof(offset), offset,
                    "Offset must be a whole number of minutes.");
            if (o <= TimeSpan.FromHours(-24) || o >= TimeSpan.FromHours(24))
                throw new ArgumentOutOfRangeException(nameof(offset), offset,
                    "Offset must be greater than -24:00 and less than +24:00.");
        }

        Year = year;
        Month = month;
        Day = day;
        Hour = hour;
        Minute = minute;
        Second = second;
        Nanosecond = nanosecond;
        Offset = offset;
    }

    /// <summary>
    /// Factory equivalent of the validating constructor.
    /// </summary>
    /// <inheritdoc cref="ExtendedIsoDateTime(long,int,int,int,int,int,int,System.Nullable{System.TimeSpan})"/>
    public static ExtendedIsoDateTime Create(
        long year,
        int month,
        int day,
        int hour = 0,
        int minute = 0,
        int second = 0,
        int nanosecond = 0,
        TimeSpan? offset = null)
        => new(year, month, day, hour, minute, second, nanosecond, offset);

    #region Calendar helpers

    /// <summary>
    /// Returns whether <paramref name="year"/> is a leap year under the proleptic Gregorian rules
    /// (divisible by 4, except by 100, except by 400). Valid for negative and zero years.
    /// </summary>
    public static bool IsLeapYear(long year)
        => (year % 4 == 0 && year % 100 != 0) || year % 400 == 0;

    /// <summary>Returns the number of days in the given month of the given year.</summary>
    public static int DaysInMonth(long year, int month)
    {
        if (month is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(month), month, "Month must be between 1 and 12.");
        if (month == 2 && IsLeapYear(year))
            return 29;
        return DaysInMonthCommon[month - 1];
    }

    /// <summary>
    /// Converts a civil (year, month, day) date to a serial day number where 0 == 1970-01-01.
    /// Based on Howard Hinnant's public-domain <c>days_from_civil</c> algorithm; valid for the
    /// whole proleptic Gregorian calendar including negative years.
    /// </summary>
    internal static long DaysFromCivil(long y, int m, int d)
    {
        y -= m <= 2 ? 1 : 0;
        long era = (y >= 0 ? y : y - 399) / 400;
        long yoe = y - era * 400;                                   // [0, 399]
        long doy = (153L * (m + (m > 2 ? -3 : 9)) + 2) / 5 + d - 1; // [0, 365]
        long doe = yoe * 365 + yoe / 4 - yoe / 100 + doy;           // [0, 146096]
        return era * 146097 + doe - 719468;
    }

    /// <summary>
    /// Inverse of <see cref="DaysFromCivil"/>. Converts a serial day number (0 == 1970-01-01)
    /// back to a civil (year, month, day) date. Based on Hinnant's <c>civil_from_days</c>.
    /// </summary>
    internal static (long year, int month, int day) CivilFromDays(long z)
    {
        z += 719468;
        long era = (z >= 0 ? z : z - 146096) / 146097;
        long doe = z - era * 146097;                                 // [0, 146096]
        long yoe = (doe - doe / 1460 + doe / 36524 - doe / 146096) / 365; // [0, 399]
        long y = yoe + era * 400;
        long doy = doe - (365 * yoe + yoe / 4 - yoe / 100);          // [0, 365]
        long mp = (5 * doy + 2) / 153;                               // [0, 11]
        int d = (int)(doy - (153 * mp + 2) / 5 + 1);                 // [1, 31]
        int m = (int)(mp < 10 ? mp + 3 : mp - 9);                    // [1, 12]
        return (y + (m <= 2 ? 1 : 0), m, d);
    }

    /// <summary>The serial day number (0 == 1970-01-01) of this value's calendar date.</summary>
    private long DayNumber => DaysFromCivil(Year, Month, Day);

    /// <summary>Nanoseconds elapsed since midnight for this value's time-of-day.</summary>
    private long NanosecondOfDay
        => ((long)((Hour * 60 + Minute) * 60 + Second)) * NanosecondsPerSecond + Nanosecond;

    #endregion

    #region Arithmetic

    /// <summary>
    /// Returns a new value advanced by <paramref name="days"/> calendar days. Time-of-day and
    /// offset are preserved. Negative values move backwards.
    /// </summary>
    public ExtendedIsoDateTime AddDays(long days)
    {
        (long y, int m, int d) = CivilFromDays(DayNumber + days);
        return new ExtendedIsoDateTime(y, m, d, Hour, Minute, Second, Nanosecond, Offset);
    }

    /// <summary>
    /// Returns a new value with <paramref name="months"/> months added. If the resulting month has
    /// fewer days than <see cref="Day"/>, the day is clamped to the last day of that month
    /// (for example, Jan 31 + 1 month → Feb 28/29).
    /// </summary>
    public ExtendedIsoDateTime AddMonths(long months)
    {
        long total = Year * 12 + (Month - 1) + months;
        long q = total / 12;
        long r = total % 12;
        if (r < 0)
        {
            r += 12;
            q -= 1;
        }

        long newYear = q;
        int newMonth = (int)r + 1;
        int newDay = Math.Min(Day, DaysInMonth(newYear, newMonth));
        return new ExtendedIsoDateTime(newYear, newMonth, newDay, Hour, Minute, Second, Nanosecond, Offset);
    }

    /// <summary>
    /// Returns a new value with <paramref name="years"/> years added. Feb 29 in a leap year is
    /// clamped to Feb 28 when the target year is not a leap year.
    /// </summary>
    public ExtendedIsoDateTime AddYears(long years)
    {
        long newYear = Year + years;
        int newDay = Math.Min(Day, DaysInMonth(newYear, Month));
        return new ExtendedIsoDateTime(newYear, Month, newDay, Hour, Minute, Second, Nanosecond, Offset);
    }

    /// <summary>
    /// Returns the signed difference <c>this - other</c> as a <see cref="TimeSpan"/>, comparing the
    /// two values as instants on the UTC time line.
    /// </summary>
    /// <remarks>
    /// Values with an unspecified offset are treated as if they were UTC. <see cref="TimeSpan"/> has
    /// only 100-nanosecond (tick) resolution, so any finer fractional difference is truncated. The
    /// method throws when the difference does not fit into a <see cref="TimeSpan"/>.
    /// </remarks>
    /// <exception cref="OverflowException">The difference is too large for a <see cref="TimeSpan"/>.</exception>
    public TimeSpan Difference(ExtendedIsoDateTime other)
    {
        (long aDays, long aNanos) = ToUtcInstant();
        (long bDays, long bNanos) = other.ToUtcInstant();

        checked
        {
            long dayDiff = aDays - bDays;
            long nanoDiff = aNanos - bNanos;
            long totalNanos = dayDiff * NanosecondsPerDay + nanoDiff;
            return TimeSpan.FromTicks(totalNanos / TicksPerNanosecondDivisor);
        }
    }

    /// <summary>
    /// Returns the whole number of calendar days between the two values' dates
    /// (<c>this - other</c>), ignoring time-of-day and offset.
    /// </summary>
    public long DaysBetween(ExtendedIsoDateTime other) => DayNumber - other.DayNumber;

    #endregion

    #region Instant / comparison

    /// <summary>
    /// Normalizes this value to a UTC instant expressed as (serial day number, nanosecond of day).
    /// Unspecified offsets are treated as UTC.
    /// </summary>
    private (long days, long nanoOfDay) ToUtcInstant()
    {
        long days = DayNumber;
        long nanos = NanosecondOfDay;

        if (Offset is TimeSpan o)
            nanos -= o.Ticks * TicksPerNanosecondDivisor;

        // Re-normalize the nanosecond-of-day into [0, NanosecondsPerDay) using floored division
        // so that a negative result borrows a day.
        long carry = nanos / NanosecondsPerDay;
        nanos -= carry * NanosecondsPerDay;
        if (nanos < 0)
        {
            nanos += NanosecondsPerDay;
            carry -= 1;
        }

        return (days + carry, nanos);
    }

    /// <inheritdoc />
    public int CompareTo(ExtendedIsoDateTime? other)
    {
        if (other is null) return 1;
        (long aDays, long aNanos) = ToUtcInstant();
        (long bDays, long bNanos) = other.ToUtcInstant();
        int byDay = aDays.CompareTo(bDays);
        return byDay != 0 ? byDay : aNanos.CompareTo(bNanos);
    }

    int IComparable.CompareTo(object? obj)
    {
        if (obj is null) return 1;
        if (obj is ExtendedIsoDateTime other) return CompareTo(other);
        throw new ArgumentException($"Object must be of type {nameof(ExtendedIsoDateTime)}.", nameof(obj));
    }

    /// <summary>
    /// Determines instant equality: two values are equal when they denote the same point on the UTC
    /// time line. Unspecified offsets are treated as UTC. Use <see cref="EqualsExact"/> for a
    /// component-by-component comparison.
    /// </summary>
    public bool Equals(ExtendedIsoDateTime? other)
    {
        if (other is null) return false;
        return ToUtcInstant() == other.ToUtcInstant();
    }

    /// <summary>
    /// Determines whether every component (including the offset and its specified/unspecified state)
    /// matches exactly. <c>...+00:00</c> and <c>...Z</c> are considered the same offset (both zero).
    /// </summary>
    public bool EqualsExact(ExtendedIsoDateTime? other)
    {
        if (other is null) return false;
        return Year == other.Year
            && Month == other.Month
            && Day == other.Day
            && Hour == other.Hour
            && Minute == other.Minute
            && Second == other.Second
            && Nanosecond == other.Nanosecond
            && Nullable.Equals(Offset, other.Offset);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ExtendedIsoDateTime other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        (long days, long nanos) = ToUtcInstant();
        return HashCode.Combine(days, nanos);
    }

    /// <summary>Instant-equality operator. See <see cref="Equals(ExtendedIsoDateTime)"/>.</summary>
    public static bool operator ==(ExtendedIsoDateTime? left, ExtendedIsoDateTime? right)
        => left is null ? right is null : left.Equals(right);

    /// <summary>Instant-inequality operator.</summary>
    public static bool operator !=(ExtendedIsoDateTime? left, ExtendedIsoDateTime? right)
        => !(left == right);

    /// <summary>Less-than instant comparison.</summary>
    public static bool operator <(ExtendedIsoDateTime left, ExtendedIsoDateTime right)
        => left.CompareTo(right) < 0;

    /// <summary>Greater-than instant comparison.</summary>
    public static bool operator >(ExtendedIsoDateTime left, ExtendedIsoDateTime right)
        => left.CompareTo(right) > 0;

    /// <summary>Less-than-or-equal instant comparison.</summary>
    public static bool operator <=(ExtendedIsoDateTime left, ExtendedIsoDateTime right)
        => left.CompareTo(right) <= 0;

    /// <summary>Greater-than-or-equal instant comparison.</summary>
    public static bool operator >=(ExtendedIsoDateTime left, ExtendedIsoDateTime right)
        => left.CompareTo(right) >= 0;

    #endregion

    #region Deconstruct

    /// <summary>Deconstructs the value into its date components.</summary>
    public void Deconstruct(out long year, out int month, out int day)
    {
        year = Year;
        month = Month;
        day = Day;
    }

    /// <summary>Deconstructs the value into all of its components.</summary>
    public void Deconstruct(
        out long year,
        out int month,
        out int day,
        out int hour,
        out int minute,
        out int second,
        out int nanosecond,
        out TimeSpan? offset)
    {
        year = Year;
        month = Month;
        day = Day;
        hour = Hour;
        minute = Minute;
        second = Second;
        nanosecond = Nanosecond;
        offset = Offset;
    }

    #endregion

    #region Parsing

    /// <summary>
    /// Parses an ISO-8601 / RFC-3339 extended date-time string. The year may use the four-digit
    /// form (0000–9999) or the ISO expanded form with an explicit sign (for example <c>+010000</c>
    /// or <c>-000001</c>). The offset may be <c>Z</c>, <c>+HH:mm</c>, <c>-HH:mm</c>, or omitted.
    /// </summary>
    /// <exception cref="FormatException">The input is not a valid extended date-time.</exception>
    public static ExtendedIsoDateTime Parse(string text)
    {
        if (text is null) throw new ArgumentNullException(nameof(text));
        if (!TryParse(text, out ExtendedIsoDateTime? value))
            throw new FormatException($"'{text}' is not a valid ISO-8601 extended date-time.");
        return value!;
    }

    /// <summary>
    /// Attempts to parse an ISO-8601 / RFC-3339 extended date-time string. Returns
    /// <see langword="false"/> instead of throwing on malformed or out-of-range input.
    /// </summary>
    public static bool TryParse(string? text, out ExtendedIsoDateTime? value)
    {
        value = null;
        if (string.IsNullOrEmpty(text))
            return false;

        ReadOnlySpan<char> s = text.AsSpan();
        int pos = 0;

        // ---- Year (optional sign, then digits) ----
        int sign = 1;
        bool signed = false;
        if (s[pos] == '+' || s[pos] == '-')
        {
            signed = true;
            sign = s[pos] == '-' ? -1 : 1;
            pos++;
        }

        int yearStart = pos;
        while (pos < s.Length && char.IsAsciiDigit(s[pos])) pos++;
        int yearDigits = pos - yearStart;

        // Unsigned years must be exactly 4 digits; signed (expanded) years need at least 4.
        if (signed)
        {
            if (yearDigits < 4) return false;
        }
        else if (yearDigits != 4)
        {
            return false;
        }

        if (!long.TryParse(s.Slice(yearStart, yearDigits), NumberStyles.None, CultureInfo.InvariantCulture, out long absYear))
            return false;
        long year = sign * absYear;

        if (!Expect(s, ref pos, '-')) return false;
        if (!TryReadFixedDigits(s, ref pos, 2, out int month)) return false;
        if (!Expect(s, ref pos, '-')) return false;
        if (!TryReadFixedDigits(s, ref pos, 2, out int day)) return false;

        // ---- Date/time separator ----
        if (pos >= s.Length || !(s[pos] == 'T' || s[pos] == 't' || s[pos] == ' ')) return false;
        pos++;

        if (!TryReadFixedDigits(s, ref pos, 2, out int hour)) return false;
        if (!Expect(s, ref pos, ':')) return false;
        if (!TryReadFixedDigits(s, ref pos, 2, out int minute)) return false;
        if (!Expect(s, ref pos, ':')) return false;
        if (!TryReadFixedDigits(s, ref pos, 2, out int second)) return false;

        // ---- Optional fractional seconds ----
        int nanosecond = 0;
        if (pos < s.Length && s[pos] == '.')
        {
            pos++;
            int fracStart = pos;
            while (pos < s.Length && char.IsAsciiDigit(s[pos])) pos++;
            int fracDigits = pos - fracStart;
            if (fracDigits is < 1 or > 9) return false;

            if (!int.TryParse(s.Slice(fracStart, fracDigits), NumberStyles.None, CultureInfo.InvariantCulture, out int frac))
                return false;

            // Scale to nanoseconds (right-pad to 9 digits).
            for (int i = fracDigits; i < 9; i++) frac *= 10;
            nanosecond = frac;
        }

        // ---- Optional offset ----
        TimeSpan? offset = null;
        if (pos < s.Length)
        {
            char c = s[pos];
            if (c == 'Z' || c == 'z')
            {
                offset = TimeSpan.Zero;
                pos++;
            }
            else if (c == '+' || c == '-')
            {
                int offSign = c == '-' ? -1 : 1;
                pos++;
                if (!TryReadFixedDigits(s, ref pos, 2, out int offHours)) return false;
                if (!Expect(s, ref pos, ':')) return false;
                if (!TryReadFixedDigits(s, ref pos, 2, out int offMinutes)) return false;
                if (offHours > 23 || offMinutes > 59) return false;
                offset = new TimeSpan(offSign * offHours, offSign * offMinutes, 0);
            }
            else
            {
                return false;
            }
        }

        if (pos != s.Length) return false;

        try
        {
            value = new ExtendedIsoDateTime(year, month, day, hour, minute, second, nanosecond, offset);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static bool Expect(ReadOnlySpan<char> s, ref int pos, char expected)
    {
        if (pos < s.Length && s[pos] == expected)
        {
            pos++;
            return true;
        }
        return false;
    }

    private static bool TryReadFixedDigits(ReadOnlySpan<char> s, ref int pos, int count, out int value)
    {
        value = 0;
        if (pos + count > s.Length) return false;
        int acc = 0;
        for (int i = 0; i < count; i++)
        {
            char c = s[pos + i];
            if (!char.IsAsciiDigit(c)) return false;
            acc = acc * 10 + (c - '0');
        }
        pos += count;
        value = acc;
        return true;
    }

    #endregion

    #region Formatting

    /// <summary>
    /// Formats the value using the ISO-8601 / RFC-3339 extended representation. Identical to
    /// <see cref="ToStringIso"/>.
    /// </summary>
    public override string ToString() => ToStringIso();

    /// <summary>
    /// Formats the value as an ISO-8601 / RFC-3339 extended date-time string.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><description>Years 0000–9999 are written as four digits without a sign.</description></item>
    ///   <item><description>Years &lt; 0 or &gt; 9999 use the ISO expanded form with an explicit
    ///   sign and at least six digits (for example <c>+010000</c>, <c>-000001</c>).</description></item>
    ///   <item><description>A zero offset is written as <c>Z</c>; non-zero offsets as <c>+HH:mm</c> /
    ///   <c>-HH:mm</c>; an unspecified offset emits nothing.</description></item>
    ///   <item><description>Fractional seconds use the fewest digits possible (trailing zeros are
    ///   removed) and are omitted entirely when zero.</description></item>
    /// </list>
    /// </remarks>
    public string ToStringIso()
    {
        var sb = new StringBuilder(32);

        // Year
        if (Year is >= 0 and <= 9999)
        {
            sb.Append(Year.ToString("D4", CultureInfo.InvariantCulture));
        }
        else
        {
            sb.Append(Year < 0 ? '-' : '+');
            long abs = Year < 0 ? -Year : Year;
            sb.Append(abs.ToString("D6", CultureInfo.InvariantCulture));
        }

        sb.Append('-').Append(Month.ToString("D2", CultureInfo.InvariantCulture));
        sb.Append('-').Append(Day.ToString("D2", CultureInfo.InvariantCulture));
        sb.Append('T');
        sb.Append(Hour.ToString("D2", CultureInfo.InvariantCulture));
        sb.Append(':').Append(Minute.ToString("D2", CultureInfo.InvariantCulture));
        sb.Append(':').Append(Second.ToString("D2", CultureInfo.InvariantCulture));

        if (Nanosecond != 0)
        {
            string frac = Nanosecond.ToString("D9", CultureInfo.InvariantCulture).TrimEnd('0');
            sb.Append('.').Append(frac);
        }

        if (Offset is TimeSpan o)
        {
            if (o == TimeSpan.Zero)
            {
                sb.Append('Z');
            }
            else
            {
                sb.Append(o < TimeSpan.Zero ? '-' : '+');
                TimeSpan abs = o.Duration();
                sb.Append(((int)abs.TotalHours).ToString("D2", CultureInfo.InvariantCulture));
                sb.Append(':').Append(abs.Minutes.ToString("D2", CultureInfo.InvariantCulture));
            }
        }

        return sb.ToString();
    }

    #endregion

    #region .NET interop

    /// <summary>
    /// Converts this value to a <see cref="System.DateTimeOffset"/>.
    /// </summary>
    /// <remarks>
    /// This is only possible when the value has a fixed offset and its date falls within the
    /// <see cref="System.DateTimeOffset"/> supported range (years 1–9999). Precision finer than
    /// 100 nanoseconds (one tick) is truncated.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The offset is unspecified.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the supported range.</exception>
    public System.DateTimeOffset ToDateTimeOffset()
    {
        if (Offset is not TimeSpan o)
            throw new InvalidOperationException(
                "Cannot convert a value with an unspecified offset to DateTimeOffset.");

        if (Year is < 1 or > 9999)
            throw new ArgumentOutOfRangeException(nameof(Year), Year,
                "The year is outside the range supported by DateTimeOffset (1–9999).");

        long daysFrom0001 = DayNumber - DayNumberOfYear0001;
        long ticks = daysFrom0001 * TimeSpan.TicksPerDay + NanosecondOfDay / TicksPerNanosecondDivisor;

        if (ticks < 0 || ticks > System.DateTime.MaxValue.Ticks)
            throw new ArgumentOutOfRangeException(nameof(ticks), ticks,
                "The value is outside the range supported by DateTimeOffset.");

        var local = new System.DateTime(ticks, DateTimeKind.Unspecified);
        return new System.DateTimeOffset(local, o);
    }

    /// <summary>
    /// Attempts to convert this value to a <see cref="System.DateTimeOffset"/>. Returns
    /// <see langword="false"/> when the offset is unspecified or the value is out of range.
    /// </summary>
    public bool TryToDateTimeOffset(out System.DateTimeOffset value)
    {
        try
        {
            value = ToDateTimeOffset();
            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            value = default;
            return false;
        }
    }

    /// <summary>
    /// Creates an <see cref="ExtendedIsoDateTime"/> from a <see cref="System.DateTimeOffset"/>,
    /// preserving its wall-clock components and fixed offset.
    /// </summary>
    public static ExtendedIsoDateTime FromDateTimeOffset(System.DateTimeOffset value)
    {
        System.DateTime local = value.DateTime; // wall-clock time, Kind == Unspecified
        int nanosecond = (int)(local.Ticks % TimeSpan.TicksPerSecond) * (int)TicksPerNanosecondDivisor;
        return new ExtendedIsoDateTime(
            local.Year, local.Month, local.Day,
            local.Hour, local.Minute, local.Second,
            nanosecond, value.Offset);
    }

    /// <summary>
    /// Creates an <see cref="ExtendedIsoDateTime"/> from a <see cref="System.DateTime"/>.
    /// </summary>
    /// <remarks>
    /// The resulting offset depends on <see cref="System.DateTime.Kind"/>:
    /// <list type="bullet">
    ///   <item><description><see cref="DateTimeKind.Utc"/> → offset <c>Z</c> (zero).</description></item>
    ///   <item><description><see cref="DateTimeKind.Local"/> → the local time zone's offset at that instant.</description></item>
    ///   <item><description><see cref="DateTimeKind.Unspecified"/> → unspecified offset.</description></item>
    /// </list>
    /// </remarks>
    public static ExtendedIsoDateTime FromDateTime(System.DateTime value)
    {
        TimeSpan? offset = value.Kind switch
        {
            DateTimeKind.Utc => TimeSpan.Zero,
            DateTimeKind.Local => TimeZoneInfo.Local.GetUtcOffset(value),
            _ => null,
        };

        int nanosecond = (int)(value.Ticks % TimeSpan.TicksPerSecond) * (int)TicksPerNanosecondDivisor;
        return new ExtendedIsoDateTime(
            value.Year, value.Month, value.Day,
            value.Hour, value.Minute, value.Second,
            nanosecond, offset);
    }

    #endregion
}
