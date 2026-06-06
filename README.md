# Broiler.DateTime

A small C# / .NET library for **ISO-8601 / RFC-3339-like date-time values that reach beyond
the .NET `DateTime` / `DateTimeOffset` year range** — including ISO *expanded* years such as
`+010000` and negative / astronomical years such as `-000001`.

It is intentionally minimal: the proleptic Gregorian calendar, astronomical year numbering, and
**fixed UTC offsets only**. No time-zone database, no locale-specific calendars, no leap seconds.

- **Target framework:** .NET 10.0
- **License:** [Apache-2.0](LICENSE)
- **Core type:** `Broiler.DateTime.ExtendedIsoDateTime`

---

## Why?

.NET's `DateTime` and `DateTimeOffset` are limited to years `0001`–`9999`. Some data formats
(astronomical software, geology/paleontology timelines, ISO-8601 expanded representations,
JavaScript `Date` extremes) need to represent dates outside that window. `Broiler.DateTime`
fills that gap with a single immutable value type.

---

## Quick start

```csharp
using Broiler.DateTime;

// Parse a familiar RFC-3339 timestamp
var t = ExtendedIsoDateTime.Parse("2025-06-05T14:30:00.123456789Z");

Console.WriteLine(t.Year);        // 2025
Console.WriteLine(t.Nanosecond);  // 123456789
Console.WriteLine(t.ToStringIso()); // 2025-06-05T14:30:00.123456789Z

// Expanded ISO year (10000)
var future = ExtendedIsoDateTime.Parse("+010000-01-01T00:00:00Z");

// Negative / astronomical year (-1 == 2 BCE)
var ancient = ExtendedIsoDateTime.Parse("-000001-01-01T00:00:00Z");

// Construct directly (validated)
var x = new ExtendedIsoDateTime(
    year: 2025, month: 6, day: 5,
    hour: 14, minute: 30, second: 0,
    nanosecond: 0, offset: TimeSpan.FromHours(2));

// Deconstruct
var (year, month, day) = x;
var (y, mo, d, h, mi, s, ns, off) = x;
```

### Arithmetic

```csharp
var start = ExtendedIsoDateTime.Parse("2025-01-31T00:00:00Z");

start.AddDays(1);     // 2025-02-01T00:00:00Z  (crosses month boundary)
start.AddMonths(1);   // 2025-02-28T00:00:00Z  (clamped end-of-month)
start.AddYears(1);    // 2026-01-31T00:00:00Z

// Difference as a TimeSpan (instant-based, offset-aware)
var a = ExtendedIsoDateTime.Parse("2025-06-05T14:00:00Z");
var b = ExtendedIsoDateTime.Parse("2025-06-05T12:00:00Z");
TimeSpan delta = a.Difference(b);   // 02:00:00

// Whole calendar days between two dates
long days = a.DaysBetween(b);       // 0
```

### Equality & comparison

Equality and ordering are **instant-based**: two values that denote the same point on the UTC
time line are equal, even if their offsets differ. Use `EqualsExact` for a component-by-component
comparison.

```csharp
var z   = ExtendedIsoDateTime.Parse("2025-06-05T12:00:00Z");
var off = ExtendedIsoDateTime.Parse("2025-06-05T14:00:00+02:00");

z == off            // true  (same instant)
z.EqualsExact(off)  // false (different wall-clock + offset)
```

### Interop with `DateTime` / `DateTimeOffset`

```csharp
// To DateTimeOffset — only when there is an offset AND the value fits .NET's 1–9999 range
var dto = ExtendedIsoDateTime.Parse("2025-06-05T14:30:00+02:00").ToDateTimeOffset();

if (ExtendedIsoDateTime.Parse("+010000-01-01T00:00:00Z").TryToDateTimeOffset(out var d))
{
    // not reached — out of range
}

// From the BCL
var fromDto = ExtendedIsoDateTime.FromDateTimeOffset(DateTimeOffset.UtcNow);
var fromDt  = ExtendedIsoDateTime.FromDateTime(DateTime.UtcNow); // Utc -> Z
```

### Unix / ECMAScript time value

`ToUnixTimeMilliseconds` / `FromUnixTimeMilliseconds` convert to and from the number of
milliseconds since `1970-01-01T00:00:00Z` — the ECMAScript `Date` time value. They use
`double` (not `long` like the BCL) so the value matches a JavaScript `Date` exactly, and
they span the **full year range** rather than `DateTimeOffset`'s 1–9999 window. A value with
an unspecified offset is treated as UTC; `FromUnixTimeMilliseconds` produces a `Z` value.

```csharp
ExtendedIsoDateTime.Parse("1970-01-01T00:00:00Z").ToUnixTimeMilliseconds(); // 0

// ECMAScript Date extremes round-trip through the expanded-year forms
ExtendedIsoDateTime.FromUnixTimeMilliseconds(8.64e15).ToStringIso();  // +275760-09-13T00:00:00Z
ExtendedIsoDateTime.FromUnixTimeMilliseconds(-8.64e15).ToStringIso(); // -271821-04-20T00:00:00Z
```

### JSON (System.Text.Json)

The type is annotated with `[JsonConverter(typeof(ExtendedIsoDateTimeJsonConverter))]`, so it
serializes to / from an ISO string out of the box:

```csharp
string json = JsonSerializer.Serialize(
    ExtendedIsoDateTime.Parse("2025-06-05T14:30:00.123Z"));
// "2025-06-05T14:30:00.123Z"

var value = JsonSerializer.Deserialize<ExtendedIsoDateTime>(
    "\"+010000-01-01T00:00:00Z\"");
```

---

## Supported string formats

Parsing accepts (lowercase `t`/`z` and a space date/time separator are also tolerated):

| Example                               | Notes                              |
| ------------------------------------- | ---------------------------------- |
| `2025-06-05T14:30:00Z`                | UTC                                |
| `2025-06-05T14:30:00.123Z`            | milliseconds                       |
| `2025-06-05T14:30:00.123456789Z`      | nanoseconds                        |
| `2025-06-05T14:30:00+02:00`           | fixed offset                       |
| `2025-06-05T14:30:00`                 | unspecified offset                 |
| `+010000-01-01T00:00:00Z`             | ISO expanded positive year         |
| `-000001-01-01T00:00:00Z`             | negative (astronomical) year       |
| `0000-01-01T00:00:00Z`                | year zero                          |

Four-digit years must not carry a sign; signed (expanded) years require at least four digits and
are conventionally written with six.

### Formatting rules (`ToString` / `ToStringIso`)

- Years **0000–9999** are written as four digits, **no sign**.
- Years **< 0 or > 9999** use the ISO expanded form with an **explicit sign** and **at least six
  digits** (e.g. `+010000`, `-000001`).
- A zero offset is written as `Z`; non-zero offsets as `+HH:mm` / `-HH:mm`.
- An unspecified offset emits nothing.
- Fractional seconds use the **fewest digits possible** (trailing zeros trimmed) and are omitted
  entirely when zero.

---

## Calendar & year-numbering semantics

`Broiler.DateTime` uses the **proleptic Gregorian calendar** (the Gregorian rules projected
backwards before their historical introduction) together with **astronomical year numbering**:

- **Year `0` exists.** It corresponds to 1 BCE.
- **Year `-1`** corresponds to 2 BCE, `-2` to 3 BCE, and so on.
- This differs from the "historical" convention where the year before 1 CE is 1 BCE with no
  year zero. If your source data uses 1 BCE / 2 BCE labels, subtract one and negate to get the
  astronomical year (1 BCE → `0`, 2 BCE → `-1`).

**Leap-year rule** (applied to all years, including `0` and negatives): a year is a leap year
when it is divisible by 4, **except** years divisible by 100, **except** years divisible by 400.

The date ⇄ day-number conversions use Howard Hinnant's well-known public-domain civil-calendar
algorithms, which are exact across the entire signed range. The implementation deliberately avoids
using the BCL `DateTime` for core calculations.

---

## Edge cases worth knowing

- **`Z` vs `+00:00`** — both parse to a zero offset and format back as `Z`. They are
  `EqualsExact`-equal.
- **`Difference` precision** — `TimeSpan` has 100-nanosecond (tick) resolution, so any finer
  fractional difference is truncated. It also throws `OverflowException` for differences that do
  not fit in a `TimeSpan`.
- **`ToDateTimeOffset` precision** — sub-tick (finer than 100 ns) fractions are truncated, and
  the call requires both a fixed offset and a year in `1`–`9999`.
- **End-of-month clamping** — `AddMonths`/`AddYears` clamp the day to the last valid day of the
  target month (Jan 31 + 1 month → Feb 28/29; Feb 29 + 1 year → Feb 28).
- **Unspecified offset in comparisons** — values without an offset are treated as if they were
  UTC for the purposes of `Difference`, equality, and ordering.

---

## Limitations

This library is deliberately small. It does **not** provide:

- **No IANA / Olson time-zone support.** Only fixed UTC offsets. There is no DST handling, no
  zone identifiers (`Europe/Paris`), and no historical offset transitions.
- **No locale-specific or alternative calendars.** Proleptic Gregorian only — no Julian, Hebrew,
  Islamic, Japanese-era, etc.
- **No leap seconds.** Seconds are limited to `0`–`59`; `:60` is rejected.
- **Not a drop-in replacement for `DateTime`.** It targets a narrow problem (extended-range ISO
  values) and does not aim to reproduce the full BCL date/time surface area.

---

## Building & testing

```bash
dotnet build
dotnet test
```

The solution contains:

```
Broiler.DateTime.sln
src/Broiler.DateTime/            # the library
tests/Broiler.DateTime.Tests/    # xUnit test suite
```

---

## License

Licensed under the [Apache License 2.0](LICENSE).
