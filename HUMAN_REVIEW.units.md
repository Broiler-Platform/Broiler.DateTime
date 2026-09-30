# Human Review: Broiler.DateTime

GENERATED - DO NOT EDIT MANUALLY. Regenerate with
`dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance generate --root Broiler.DateTime`, which rewrites this file,
`CODE-ASSURANCE.md`, `assurance.manifest.json` and every generated source header from the
product tree.

> **Status: PENDING.** Human-reviewed: 0 of 64 relevant units. `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.DateTime --release`
> fails while any relevant unit is without a decision bound to its current fingerprint.

## 1. How To Use This File

Read it; do not edit it. A decision about a code unit is the `// Broiler-Human:` line on that
unit's declaration, and every table below is read out of those lines. There is nothing here
to fill in and nothing here to leave blank.

## 2. How A Review Is Recorded

In one place: the `// Broiler-Human:` line of the assurance annotation that sits on the
declaration being read. Nothing in this file is edited by hand, no second document carries a
per-item checklist, and no list of permitted aliases exists to be added to.

```csharp
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=2; Fingerprint=4A3BFD
// Broiler-Falsified-If: a negative value reaches the running total
// Broiler-Human:        PENDING
```

The last line has four shapes. A human writes three of them; the generator writes the fourth
and may never invent an alias, which the check asserts in both directions.

| Line | Meaning |
|---|---|
| `PENDING` | Nobody has recorded a decision for this unit. The generator leaves it exactly as it stands. |
| `<alias>` | A human states their own alias and leaves the machine field to the generator, which fills it with the declaration's fingerprint at the next run. |
| `<alias>; Fingerprint=<six hex>` | A decision bound to one exact version of one declaration. |
| `STALE; Previous=<alias>@<fingerprint>` | Written by the generator when the code moved after a decision. Only a human clears it, by stating their alias again. |

A human may state their own `IP=`, `Security=` and `Resources=` assessment beside their alias,
which is how a reader disagrees with the machine assessment on the line above: an assessment is
a comment and moves no fingerprint, so there is nowhere else to say it.

**No branch, commit or tag is recorded in this file.** Each decision names the fingerprint of
the declaration it was made against, and the state machine compares that value with the
declaration as it now stands. A commit says a tree moved; a fingerprint says whether this unit
did, which is the narrower and the more useful of the two.

## 3. Summary

| Metric | Value |
|---|---:|
| Files scanned | 2 |
| Code units | 76 |
| Relevant | 64 |
| Exempt | 12 |
| Assessed | 64 of 64 (100%) |
| Human reviewed | 0 of 64 (0%) |
| Unverified | 64 |
| Aliases naming a decision | 0 |

## 4. Review States

One row per state of the machine that reads the two lines. The states are computed from the
annotations and the current fingerprints; nothing stores them.

| State | Units |
|---|---:|
| NEW | 0 |
| AI_ASSESSED | 0 |
| HUMAN_PENDING | 64 |
| HUMAN_APPROVED_PENDING_FINGERPRINT | 0 |
| VERIFIED | 0 |
| STALE | 0 |
| EXEMPT | 12 |

## 5. Aliases In The Tree

No alias appears on a human line anywhere in the product tree. Nobody has recorded a
decision about any unit of this component.

## 6. Coverage By File

One row per covered file, carrying that file's generated header. `Unverified` counts the
relevant units in a state that blocks a release.

| File | Units | Relevant | Exempt | Unverified | IP risk | Security risk | Criteria |
|---|---:|---:|---:|---:|---|---|---:|
| `src/Broiler.DateTime/ExtendedIsoDateTime.cs` | 73 | 61 | 12 | 61 | Medium | Critical | 61/22 |
| `src/Broiler.DateTime/ExtendedIsoDateTimeJsonConverter.cs` | 3 | 3 | 0 | 3 | Low | Critical | 3/2 |

## 7. Decisions Recorded

No unit in this component carries a decision on its human line. Every one of them reads
`PENDING`.

## 8. Decisions The Code Has Outrun

No unit carries a decision that the code has since moved past.

## 9. Where A Decision Is Required First

The units at the top of the security vocabulary, with the observation that would show each
one wrong and the human line it carries. The set is read from the assessments rather than
written out, so a unit that becomes `High` joins it at the next generation.

- `Broiler.DateTime.ExtendedIsoDateTime` in `src/Broiler.DateTime/ExtendedIsoDateTime.cs` - Security=Critical, Spec=none cited, `253F31`, PENDING
  - Falsified if: a UTF-8 span longer than 64 bytes reaches the stackalloc char buffer in TryParse(ReadOnlySpan<byte>) instead of being rejected by its 19..64 length guard
- `Broiler.DateTime.ExtendedIsoDateTime.DaysInMonthCommon` in `src/Broiler.DateTime/ExtendedIsoDateTime.cs` - Security=High, Spec=none cited, `891DA7`, PENDING
  - Falsified if: an entry differs from the Gregorian common-year month lengths, so the parser accepts 2025-04-31 or rejects 2025-01-31
- `Broiler.DateTime.ExtendedIsoDateTime.ExtendedIsoDateTime(long, int, int, int, int, int, int, TimeSpan?)` in `src/Broiler.DateTime/ExtendedIsoDateTime.cs` - Security=High, Spec=none cited, `6CDF0F`, PENDING
  - Falsified if: a component outside its documented range (month 13, second 60, an offset of 90 seconds or exactly +24:00) yields an instance instead of ArgumentOutOfRangeException
- `Broiler.DateTime.ExtendedIsoDateTime.Validate(long, int, int, int, int, int, int, TimeSpan?)` in `src/Broiler.DateTime/ExtendedIsoDateTime.cs` - Security=High, Spec=none cited, `E46CC0`, PENDING
  - Falsified if: Validate and IsValid disagree on some tuple, e.g. one accepting 1900-02-29 or an offset of exactly -24:00 that the other rejects
- `Broiler.DateTime.ExtendedIsoDateTime.IsValid(long, int, int, int, int, int, int, TimeSpan?)` in `src/Broiler.DateTime/ExtendedIsoDateTime.cs` - Security=High, Spec=none cited, `64A2F0`, PENDING
  - Falsified if: IsValid returns true for a tuple the parser must reject, e.g. 1900-02-29, hour 24 or an offset with a non-zero seconds part
- `Broiler.DateTime.ExtendedIsoDateTime.Create(long, int, int, int, int, int, int, TimeSpan?)` in `src/Broiler.DateTime/ExtendedIsoDateTime.cs` - Security=High, Spec=none cited, `99942E`, PENDING
  - Falsified if: Create returns an instance for a tuple the validating constructor rejects, such as day 0 or nanosecond 1,000,000,000
- `Broiler.DateTime.ExtendedIsoDateTime.IsLeapYear(long)` in `src/Broiler.DateTime/ExtendedIsoDateTime.cs` - Security=High, Spec=none cited, `8BE12E`, PENDING
  - Falsified if: a negative or zero century year is misclassified, e.g. -100 reported leap or -400 or 0 reported common
- `Broiler.DateTime.ExtendedIsoDateTime.DaysInMonth(long, int)` in `src/Broiler.DateTime/ExtendedIsoDateTime.cs` - Security=High, Spec=none cited, `D40ACD`, PENDING
  - Falsified if: February of a year divisible by 100 but not 400 (1900 or -100) returns 29, so the parser accepts 29 February in it
- `Broiler.DateTime.ExtendedIsoDateTime.Parse(string)` in `src/Broiler.DateTime/ExtendedIsoDateTime.cs` - Security=High, Spec=none cited, `21CAE8`, PENDING
  - Falsified if: a string TryParse rejects (e.g. 2025-13-01T00:00:00Z) returns a value instead of throwing FormatException
- `Broiler.DateTime.ExtendedIsoDateTime.Parse(string, IFormatProvider?)` in `src/Broiler.DateTime/ExtendedIsoDateTime.cs` - Security=High, Spec=none cited, `40AF28`, PENDING
  - Falsified if: a string TryParse rejects (e.g. 2025-02-30T00:00:00Z) returns a value instead of throwing FormatException
- `Broiler.DateTime.ExtendedIsoDateTime.Parse(ReadOnlySpan<char>, IFormatProvider?)` in `src/Broiler.DateTime/ExtendedIsoDateTime.cs` - Security=High, Spec=none cited, `E57438`, PENDING
  - Falsified if: a span TryParse rejects (e.g. 2025-01-01T24:00:00Z) returns a value instead of throwing FormatException
- `Broiler.DateTime.ExtendedIsoDateTime.Parse(ReadOnlySpan<byte>, IFormatProvider?)` in `src/Broiler.DateTime/ExtendedIsoDateTime.cs` - Security=Critical, Spec=none cited, `B2EE08`, PENDING
  - Falsified if: a UTF-8 span longer than 64 bytes returns a value or reaches the stackalloc in TryParse(ReadOnlySpan<byte>) instead of throwing FormatException
- `Broiler.DateTime.ExtendedIsoDateTime.TryParse(string?, out ExtendedIsoDateTime)` in `src/Broiler.DateTime/ExtendedIsoDateTime.cs` - Security=High, Spec=none cited, `70166B`, PENDING
  - Falsified if: a null string returns true or throws instead of returning false with a null result
- `Broiler.DateTime.ExtendedIsoDateTime.TryParse(string?, IFormatProvider?, out ExtendedIsoDateTime)` in `src/Broiler.DateTime/ExtendedIsoDateTime.cs` - Security=High, Spec=none cited, `0B6ABA`, PENDING
  - Falsified if: a null string returns true or throws instead of returning false with a null result
- `Broiler.DateTime.ExtendedIsoDateTime.TryParse(ReadOnlySpan<char>, out ExtendedIsoDateTime)` in `src/Broiler.DateTime/ExtendedIsoDateTime.cs` - Security=High, Spec=none cited, `A09413`, PENDING
  - Falsified if: an empty span returns true or throws instead of returning false
- `Broiler.DateTime.ExtendedIsoDateTime.TryParse(ReadOnlySpan<char>, IFormatProvider?, out ExtendedIsoDateTime)` in `src/Broiler.DateTime/ExtendedIsoDateTime.cs` - Security=High, Spec=none cited, `220C53`, PENDING
  - Falsified if: an expanded year beyond about 2.5e16 in magnitude (e.g. +099999999999999999) is accepted, although DaysFromCivil wraps for it and the value then orders before 0001-01-01
- `Broiler.DateTime.ExtendedIsoDateTime.TryParse(ReadOnlySpan<byte>, IFormatProvider?, out ExtendedIsoDateTime)` in `src/Broiler.DateTime/ExtendedIsoDateTime.cs` - Security=Critical, Spec=none cited, `3D4933`, PENDING
  - Falsified if: a UTF-8 span outside 19..64 bytes reaches stackalloc char[utf8Text.Length] instead of returning false at the length guard
- `Broiler.DateTime.ExtendedIsoDateTime.Expect(ReadOnlySpan<char>, ref int, char)` in `src/Broiler.DateTime/ExtendedIsoDateTime.cs` - Security=High, Spec=none cited, `E8C2CD`, PENDING
  - Falsified if: a position at or past the end of the span returns true or advances pos
- `Broiler.DateTime.ExtendedIsoDateTime.TryReadFixedDigits(ReadOnlySpan<char>, ref int, int, out int)` in `src/Broiler.DateTime/ExtendedIsoDateTime.cs` - Security=High, Spec=none cited, `F39E10`, PENDING
  - Falsified if: a non-ASCII digit (e.g. fullwidth or Arabic-Indic) or a field that runs past the end of the span is accepted as a two-digit component
- `Broiler.DateTime.ExtendedIsoDateTime.TryFormat(Span<byte>, out int, ReadOnlySpan<char>, IFormatProvider?)` in `src/Broiler.DateTime/ExtendedIsoDateTime.cs` - Security=Critical, Spec=none cited, `868208`, PENDING
  - Falsified if: some value makes GetIsoStringLength return more than 51, so stackalloc char[required] grows with the stored year past its 51-character maximum
- `Broiler.DateTime.ExtendedIsoDateTime.GetIsoStringLength()` in `src/Broiler.DateTime/ExtendedIsoDateTime.cs` - Security=Critical, Spec=none cited, `56ADA3`, PENDING
  - Falsified if: for some value the returned length differs from the characters TryFormat(Span<char>) writes, or exceeds 51 (year long.MinValue with nine fraction digits and offset -01:01 must give 51)
- `Broiler.DateTime.ExtendedIsoDateTime.CountDigits(ulong)` in `src/Broiler.DateTime/ExtendedIsoDateTime.cs` - Security=Critical, Spec=none cited, `14FC8E`, PENDING
  - Falsified if: some ulong yields a count other than its decimal length, e.g. 9223372036854775808 (the magnitude of long.MinValue) not yielding 19
- `Broiler.DateTime.ExtendedIsoDateTimeJsonConverter` in `src/Broiler.DateTime/ExtendedIsoDateTimeJsonConverter.cs` - Security=Critical, Spec=none cited, `64C3BB`, PENDING
  - Falsified if: a JSON string token longer than 64 bytes reaches the stackalloc in ExtendedIsoDateTime.TryParse(ReadOnlySpan<byte>) instead of failing its length guard
- `Broiler.DateTime.ExtendedIsoDateTimeJsonConverter.Read(ref Utf8JsonReader, Type, JsonSerializerOptions)` in `src/Broiler.DateTime/ExtendedIsoDateTimeJsonConverter.cs` - Security=Critical, Spec=none cited, `11DF97`, PENDING
  - Falsified if: a string token whose raw ValueSpan exceeds 64 bytes is copied into a stack buffer sized from it by the UTF-8 TryParse fast path

## 10. What This Record Does Not Say

It is not an approval of the component, and a full table above would not be one either. It
records which declarations somebody stated a decision about, and against which version of
each. It does not record what they read, how long they spent, or whether they were right.

A fingerprint is six hex characters of SHA-256 over a declaration's token texts. It answers
whether a unit changed since a decision was recorded against it. It is not a collision-free
identifier across units and it is not a cryptographic commitment, so it detects a change and
does not resist a forger with commit access.

An assessment is a comment, so changing one moves no fingerprint anywhere, and nothing
mechanical checks that it is right; the check holds its values to their vocabularies and no
further.

62 of the 64 assessed units declare `Origin=AI`. Reading a declaration is the only thing
that makes it read.
