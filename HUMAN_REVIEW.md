# Human review: Broiler.DateTime

> **Status: APPROVED FOR PREVIEW.**

This record documents a scoped human review of Broiler.DateTime for first-preview use.
Approval means only that the named reviewer found the specified revision reasonably suitable
for the stated preview use, subject to the recorded limitations and the software license's
warranty disclaimer. "Safe" is not an absolute guarantee.

## Review target

- **Component:** Broiler.DateTime
- **Scope:** The expanded-year ISO-8601/RFC-3339-like date-time library and its tests.
- **Release:** First preview
- **Commit:** `3decee8b99b4894916f1c971cf6a2d22a2dd40ce`
- **Reviewer:** Maik Ratzmer
- **Reviewer contact or profile:** MaiRat
- **Review date:** 2026-07-01
- **Intended preview use:** Preview use as a small .NET date-time value library for expanded-year ISO-8601/RFC-3339-like parsing, formatting, arithmetic, .NET interop, and System.Text.Json serialization in non-critical or caller-validated contexts.

Any source change after the reviewed commit invalidates this approval until the changed
revision is reviewed again.

## Reviewed evidence

The human reviewer records links, logs, or concise findings for every item:

- [x] Build and automated tests completed.
- [x] Security-sensitive inputs, trust boundaries, file/network access, native interop,
      and code-execution paths were inspected where applicable.
- [x] Dependency and license notices were checked, including inherited upstream code.
- [x] AI-generated or AI-modified code received source-level review; no AI summary was
      accepted as a substitute for reading the relevant code.
- [x] Public APIs, failure behavior, known limitations, and preview compatibility risks
      were assessed.
- [x] Static analysis, dependency/vulnerability scanning, or an explicit reason for
      omitting each was recorded.
- [x] Open findings and residual risks are listed below.

### Evidence and commands

- Reviewed source files:
  - `src/Broiler.DateTime/ExtendedIsoDateTime.cs`
  - `src/Broiler.DateTime/ExtendedIsoDateTimeJsonConverter.cs`
  - `src/Broiler.DateTime/Broiler.DateTime.csproj`
  - `Directory.Build.props`
  - `README.md`
  - `LICENSE`
- Automated test command: `dotnet test Broiler.DateTime.sln`
  - Result: passed.
  - Test count: 115 passed, 0 failed, 0 skipped.
  - Target framework observed in test run: `net10.0`.
- Dependency/vulnerability command: `dotnet list Broiler.DateTime.sln package --vulnerable --include-transitive`
  - Result: no vulnerable packages were reported for `Broiler.DateTime` or `Broiler.DateTime.Tests` from the configured NuGet sources.
- Dependency and license notes:
  - The library project has no direct runtime `PackageReference` entries.
  - Test-only dependencies are `Microsoft.NET.Test.Sdk`, `xunit`, and `xunit.runner.visualstudio`.
  - Project metadata declares `Apache-2.0`; the repository includes an Apache License 2.0 `LICENSE` file.
- Static analysis notes:
  - No separate SAST tool was run for this first preview review.
  - Nullable warnings are configured as errors via `Directory.Build.props`.
  - The scoped omission is accepted because the reviewed code is a small managed .NET value-type-style library with no file I/O, network I/O, native interop, reflection-based execution, process execution, or credential handling.

### Findings and residual risks

- No blocking findings were identified for first-preview use.
- The component is generally acceptable within the reviewed preview scope.
- Security-critical exposure is considered very unlikely because the package is primarily an extended date-time value library. It does not perform file access, network access, native calls, process execution, credential handling, or dynamic code execution.
- The remaining security-relevant surface is ordinary runtime handling of parsed date-time strings and JSON serialization/deserialization through `System.Text.Json`. As with all parser and runtime-library usage, callers should continue to treat untrusted input carefully and validate behavior for their own domain.
- Date/time parsing and comparison can still affect application-level security, authorization, retention, billing, ordering, or data-integrity decisions if callers use these values in those contexts. Such consumers should add their own domain-specific tests and validation.
- Known preview limitations remain accepted risks:
  - Fixed UTC offsets only; no IANA time-zone database or daylight-saving transition handling.
  - Proleptic Gregorian calendar only.
  - Astronomical year numbering, including year zero, may differ from historical BCE/CE notation.
  - No leap-second support.
  - Sub-tick precision is truncated when converting to BCL types or `TimeSpan`.
  - Values with unspecified offsets are treated as UTC for instant comparison and difference operations.

## Decision

- [x] **APPROVED FOR PREVIEW** within the intended-use scope above.
- [ ] **APPROVED WITH CONDITIONS** listed below.
- [ ] **NOT APPROVED** for preview use.

**Conditions:** None.

## Human attestation

I confirm that I am a human developer, that I personally reviewed the revision and
evidence identified above, and that the decision is my own. I understand that this
attestation is a scoped engineering review, not a warranty or a claim that the component
is free of defects or vulnerabilities.

- **Name:** Maik Ratzmer
- **Signature or attributable commit:** MaiRat
- **Date:** 2026-07-01
