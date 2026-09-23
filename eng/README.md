# Engineering scripts

Everything needed to build, validate, and publish the package. All paths below are
relative to the repository root.

| File | Purpose |
| --- | --- |
| `Broiler.Packaging.props` | Canonical NuGet metadata shared by every Broiler component. Vendored — do not hand-edit; see the header comment. |
| `icon.png` | Package icon, packed into every `.nupkg`. |
| `run-tests.sh` | `dotnet test` against an existing build of the given configuration. |
| `pack.ps1` | Packs every packable project and verifies each resulting `.nupkg`. |
| `verify-feed.ps1` | Restores the packed release as a real consumer would, from an isolated cache. |
| `resolve-preview-version.mjs` | Chooses the next preview version. |
| `resolve-preview-version.test.mjs` | `node --test` unit tests for the resolver, run by CI. |

## Publishing

The only feed is **nuget.org**. Pushing requires the `NUGET_API_KEY` repository secret;
the publish workflow fails early if a non-dry-run is started without it.

Scope the nuget.org API key to **Push** for the glob `Broiler.*`. Because the package ID
does not exist on nuget.org until the first successful push, that first key must also
allow *Push new packages and package versions*.

There are two ways to release, both in `.github/workflows/publish.yml`:

- **`workflow_dispatch`** — the normal path. Leave *version-suffix* empty to take the
  next preview automatically. *dry-run* defaults to `true`: it builds, packs, verifies,
  and attaches the packages without pushing. Set it to `false` to publish; the workflow
  then tags the published commit `v<version>`.
- **Pushing a `v*` tag** — publishes the version named by the tag, never a dry run. The
  tag must still satisfy the rules below.

Both paths run the full CI workflow (`validate`) against the resolved version first, so
a package is only pushed if it built, tested, packed, and restored cleanly.

## How the preview version is chosen

`VersionSuffix` in `Broiler.Packaging.props` is a **floor**, not the version to publish.
The resolver collects every `X.Y.Z-preview.N` on the configured release line from two
sources and publishes the number after the highest one it finds:

1. **nuget.org**, read through the PackageBaseAddress (flat container) resource, which
   lists unlisted versions as well as listed ones.
2. **This repository's `v*` tags**, which is why the publish job tags after a successful
   push and why the version job checks out with `fetch-depth: 0`.

Neither source alone is sufficient. A tag outlives a package that was later deleted from
nuget.org — and nuget.org never re-serves a deleted version's number to a new upload — while
the feed covers a push whose tag never landed. Taking the maximum over both is what makes
the sequence cumulative: with `preview.2` on the feed and `preview.3` on a tag, the next
publish is `preview.4`, not `preview.3`.

An explicit *version-suffix* input or `v*` tag is honoured only if it is on the same
`X.Y.Z` line and is at least the number the resolver would have chosen on its own, so a
manual request can skip ahead but can never reuse or regress a number. The triggering tag
itself is excluded from the baseline, since it names the very version being published.

Raising the release line (for example to `0.2.0`) is a manual edit of `VersionPrefix`;
numbering then restarts from that line's own `VersionSuffix` floor.

## Running the pieces locally

```bash
dotnet build Broiler.DateTime.slnx -c Release
bash ./eng/run-tests.sh Release
node --test eng/resolve-preview-version.test.mjs
```

```powershell
./eng/pack.ps1                     # -> artifacts/, must be empty of packages first
./eng/verify-feed.ps1
```

`resolve-preview-version.mjs` can be run directly to see what the next version would be.
It reads nuget.org and local tags, and writes to `$GITHUB_OUTPUT` only when that variable
is set, so a local run just prints its decision.
