# Changelog

All notable changes to **Broiler.DateTime** are recorded here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## Versioning during preview

The preview line is `0.1.0-preview.N`. `N` is not written down in this file or in the
build, because it is chosen at publish time: the pipeline takes the highest number the
`0.1.0` line has ever used — across nuget.org and this repository's release tags — and
publishes the one after it. A number is therefore never reused, even if a package is
later unlisted or a publish is retried. `eng/Broiler.Packaging.props` carries the
`VersionSuffix` only as a floor for the first publish.

Because every `0.1.0-preview.N` is a snapshot of the same unstable release line, changes
are grouped under `0.1.0` rather than under individual preview numbers. Breaking changes
may land between previews without a major-version bump.

## [Unreleased]

## [0.1.0] — first preview

### Added

- `ExtendedIsoDateTime`: an immutable date-time value spanning the full signed year
  range, beyond `DateTime` / `DateTimeOffset`'s 1–9999 window.
- ISO-8601 / RFC-3339 parsing and formatting, including expanded years (`+010000`),
  negative astronomical years (`-000001`), year zero, nanosecond precision, and fixed
  UTC offsets.
- Calendar arithmetic (`AddDays`, `AddMonths`, `AddYears` with end-of-month clamping),
  `Difference`, and `DaysBetween`.
- Instant-based equality and ordering, plus `EqualsExact` for component-wise comparison.
- Interop with the BCL: `ToDateTimeOffset` / `TryToDateTimeOffset`, `FromDateTimeOffset`,
  `FromDateTime`.
- ECMAScript-compatible `ToUnixTimeMilliseconds` / `FromUnixTimeMilliseconds` over
  `double`, covering the full year range.
- `System.Text.Json` support through `ExtendedIsoDateTimeJsonConverter`, applied to the
  type by attribute.

### Known limitations

No IANA time zones, no alternative calendars, and no leap seconds. See the README's
*Limitations* section.

[Unreleased]: https://github.com/Broiler-Platform/Broiler.DateTime/commits/main
[0.1.0]: https://github.com/Broiler-Platform/Broiler.DateTime/commits/main
