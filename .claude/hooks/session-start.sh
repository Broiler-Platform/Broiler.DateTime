#!/bin/bash
# SessionStart hook: ensures a .NET 10 SDK environment for this repository.
#
# Broiler.DateTime targets net10.0 (see Directory.Build.props / global.json).
# Claude Code on the web starts from a fresh, ephemeral container, so the
# .NET 10 SDK must be (re)installed on each session start before builds/tests
# can run. This script is idempotent and safe to run multiple times.
set -euo pipefail

# Only run in the remote (Claude Code on the web) environment. Local machines
# are expected to already have the SDK installed.
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

log() { echo "[session-start] $*"; }

# Make a SDK that lives in the standard apt location discoverable.
export PATH="/usr/bin:/usr/lib/dotnet:${PATH}"

have_dotnet10() {
  command -v dotnet >/dev/null 2>&1 && dotnet --list-sdks 2>/dev/null | grep -q '^10\.'
}

if have_dotnet10; then
  log ".NET 10 SDK already present: $(dotnet --version)"
else
  log ".NET 10 SDK not found; installing via the Microsoft package feed..."

  # The Microsoft build CDN (builds.dotnet.microsoft.com / aka.ms) is blocked by
  # the environment network policy, so use the packages.microsoft.com apt feed,
  # which is reachable, to install the SDK.
  if ! apt-cache policy dotnet-sdk-10.0 2>/dev/null | grep -q 'Candidate:.*10\.'; then
    log "Registering Microsoft package repository..."
    tmpdeb="$(mktemp --suffix=.deb)"
    curl -fsSL "https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb" -o "$tmpdeb"
    sudo dpkg -i "$tmpdeb"
    rm -f "$tmpdeb"
  fi

  log "Updating apt package lists (Microsoft feed)..."
  # Limit the update to the Microsoft list so unrelated/blocked PPAs don't fail us.
  sudo apt-get update \
    -o Dir::Etc::sourcelist="sources.list.d/microsoft-prod.list" \
    -o Dir::Etc::sourceparts="-" \
    -o APT::Get::List-Cleanup="0" 2>&1 | tail -3 || sudo apt-get update 2>&1 | tail -3 || true

  log "Installing dotnet-sdk-10.0..."
  sudo DEBIAN_FRONTEND=noninteractive apt-get install -y dotnet-sdk-10.0

  if ! have_dotnet10; then
    log "ERROR: .NET 10 SDK installation did not succeed." >&2
    exit 1
  fi
  log ".NET 10 SDK installed: $(dotnet --version)"
fi

# Disable first-run noise and telemetry for a clean, fast session.
export DOTNET_NOLOGO=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# Persist environment for the rest of the session.
if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
  {
    echo 'export PATH="/usr/bin:/usr/lib/dotnet:${PATH}"'
    echo 'export DOTNET_NOLOGO=1'
    echo 'export DOTNET_CLI_TELEMETRY_OPTOUT=1'
    echo 'export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1'
  } >> "$CLAUDE_ENV_FILE"
fi

# Warm up NuGet restore so the first build/test in the session is fast.
PROJECT_DIR="${CLAUDE_PROJECT_DIR:-$(pwd)}"
if [ -f "$PROJECT_DIR/Broiler.DateTime.sln" ]; then
  log "Restoring NuGet packages..."
  dotnet restore "$PROJECT_DIR/Broiler.DateTime.sln" 2>&1 | tail -5 || \
    log "WARN: restore failed; it will be retried on first build."
fi

log "Environment ready: $(dotnet --version)"
