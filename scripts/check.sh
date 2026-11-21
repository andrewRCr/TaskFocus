#!/usr/bin/env bash
# Run one TaskFocus check from the repository root, locally, in CI or on an arc-offload runner.
#
#   scripts/check.sh <unit|integration|build-api|build-desktop|publish-web> [extra dotnet arguments...]
#
# arc-offload routes by this script's path and the subcommand, so keep both stable. The exit status is the
# check's own.
set -euo pipefail

usage() {
  echo "usage: scripts/check.sh <unit|integration|build-api|build-desktop|publish-web> [extra dotnet arguments...]" >&2
  exit 2
}
[ $# -ge 1 ] || usage
check=$1
shift

cd "$(dirname "$0")/.."

# The .NET 10 SDK: a user install wins, because a system dotnet may be older.
if [ -x "$HOME/.dotnet/dotnet" ]; then
  dotnet="$HOME/.dotnet/dotnet"
  export DOTNET_ROOT="$HOME/.dotnet"
else
  dotnet=$(command -v dotnet || true)
fi
if [ -z "$dotnet" ]; then
  echo "check.sh: no dotnet found; install the .NET 10 SDK" >&2
  exit 127
fi
sdk=$("$dotnet" --version 2>/dev/null || true)
case "$sdk" in
  10.*) ;;
  *) echo "check.sh: needs the .NET 10 SDK; $dotnet reports '${sdk:-no compatible SDK}'" >&2; exit 127 ;;
esac

export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
ulimit -n "$(ulimit -Hn)" 2>/dev/null || true

# Release builds, and no MSBuild node or compiler server outlives the run.
common=(--configuration Release --disable-build-servers -nodeReuse:false -p:UseSharedCompilation=false)

# With Ryuk off, a cancelled run leaves its SQL Server container behind. Remove test containers older than
# 30 minutes; younger ones may belong to a run that is still going.
remove_stale_test_containers() {
  local cli cutoff id day time offset created
  cli=$(command -v podman || command -v docker || true)
  [ -n "$cli" ] || return 0
  cutoff=$(( $(date +%s) - 1800 ))
  "$cli" ps --all --filter label=org.testcontainers=true --format '{{.ID}} {{.CreatedAt}}' 2>/dev/null |
    while read -r id day time offset _; do
      created=$(date -d "$day ${time%%.*} $offset" +%s 2>/dev/null) || continue
      if [ "$created" -lt "$cutoff" ]; then
        "$cli" rm --force "$id" >/dev/null 2>&1 && echo "check.sh: removed stale test container $id"
      fi
    done
}

case "$check" in
  unit)
    exec "$dotnet" test tests/TaskFocusUI.Library.Tests/TaskFocusUI.Library.Tests.csproj "${common[@]}" "$@"
    ;;
  integration)
    # Rootless Podman when its socket is there and no container host is set; CI agents use their own Docker.
    podman_socket="/run/user/$(id -u)/podman/podman.sock"
    if [ -z "${DOCKER_HOST:-}" ] && [ -S "$podman_socket" ]; then
      export DOCKER_HOST="unix://$podman_socket"
    fi
    export TESTCONTAINERS_RYUK_DISABLED=true
    remove_stale_test_containers
    exec "$dotnet" test tests/TaskFocusAPI.IntegrationTests/TaskFocusAPI.IntegrationTests.csproj "${common[@]}" "$@"
    ;;
  build-api)
    exec "$dotnet" build API/TaskFocusAPI/TaskFocusAPI.csproj "${common[@]}" "$@"
    ;;
  build-desktop)
    exec "$dotnet" build UserInterface/TaskFocusDesktop/TaskFocusDesktop.csproj "${common[@]}" \
      --runtime win-x64 -p:EnableWindowsTargeting=true "$@"
    ;;
  publish-web)
    # Inside the checkout (ignored), because an offloaded run works in a snapshot of it.
    exec "$dotnet" publish UserInterface/TaskFocusWeb/TaskFocusWeb.csproj "${common[@]}" \
      --output artifacts/publish-web "$@"
    ;;
  *)
    usage
    ;;
esac
