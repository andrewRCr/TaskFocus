# TaskFocus tests

Run from the repository root with the .NET 10 SDK. The client-library tests run
without containers. The API tests need Docker or a Docker-compatible Podman
service and permission to start a SQL Server container with a 2.5 GiB memory
limit. The first run may need to download the SQL Server image.

```sh
OUT="${OUT:-$PWD/TestResults}"
mkdir -p "$OUT/xdg"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
export DOTNET_ROOT="$HOME/.dotnet"
```

Set `DOTNET_ROOT` to your SDK location if it differs. With rootless Podman on
Linux, start its user socket and point Testcontainers at it.

```sh
systemctl --user start podman.socket
export DOCKER_HOST="unix:///run/user/$(id -u)/podman/podman.sock"
export TESTCONTAINERS_RYUK_DISABLED=true
```

Run the projects one at a time.

```sh
XDG_RUNTIME_DIR="$OUT/xdg" nice -n 15 dotnet test tests/TaskFocusUI.Library.Tests \
  --configuration Release --disable-build-servers -p:UseSharedCompilation=false \
  --logger trx --collect "XPlat Code Coverage" --results-directory "$OUT/unit"
XDG_RUNTIME_DIR="$OUT/xdg" nice -n 15 dotnet test tests/TaskFocusAPI.IntegrationTests \
  --configuration Release --disable-build-servers -p:UseSharedCompilation=false \
  --logger trx --collect "XPlat Code Coverage" --results-directory "$OUT/integration"
```

The API fixture uses a disposable SQL Server, applies the Identity migrations,
and reads the TaskFocusData scripts directly from the checkout. Email stays in
memory.

Azure Pipelines runs both projects in the Ubuntu `Test` job alongside the
Windows build and publishes TRX and Cobertura results. Deployment requires the
whole `Build` stage to succeed.
