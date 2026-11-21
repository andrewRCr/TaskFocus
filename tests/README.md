# TaskFocus tests

Run checks from the repository root with the .NET 10 SDK. `scripts/check.sh` runs each one the same way locally, in
CI and on a remote runner:

```sh
scripts/check.sh unit            # client-library tests, no containers
scripts/check.sh integration     # API tests against a SQL Server container
scripts/check.sh build-api
scripts/check.sh build-desktop   # cross-targets win-x64 when not on Windows
scripts/check.sh publish-web     # writes artifacts/publish-web
```

Extra arguments go to `dotnet`, for example
`scripts/check.sh unit --logger trx --results-directory TestResults/unit`. The script uses `~/.dotnet` when it holds
an SDK, otherwise `dotnet` on `PATH`, and stops unless the SDK is 10.x. It leaves no MSBuild or compiler server running.

The API tests need Docker or a Docker-compatible Podman service and permission to start a SQL Server container with a
2.5 GiB memory limit. The first run may download the SQL Server image. With rootless Podman on Linux, start its user
socket first (`systemctl --user start podman.socket`); the script points Testcontainers at it, turns off Ryuk, and
removes test containers a cancelled run left behind more than 30 minutes earlier.

The API fixture uses a disposable SQL Server, applies the Identity migrations, and reads the TaskFocusData scripts
directly from the checkout. Email stays in memory.

Azure Pipelines runs both test projects through the script in the Ubuntu `Test` job alongside the Windows build and
publishes TRX and Cobertura results. Deployment requires the whole `Build` stage to succeed.
