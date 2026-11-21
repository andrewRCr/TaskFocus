# Contributing to TaskFocus

TaskFocus is maintained rather than actively developed: bug fixes, security and platform updates, and small
improvements.

## Workflow

- Branch from `main` and keep each pull request to one concern.
- `main` is protected. Changes land through a pull request whose CI check, `Standard CI-CD Pipeline`, has passed, and
  pull requests are squash-merged.
- Merging to `main` deploys the API and web app to production through Azure Pipelines, so `main` must always be
  releasable. Desktop releases are published separately on GitHub Releases.

## Commit messages and pull request titles

Use [Conventional Commits](https://www.conventionalcommits.org/): `type(scope): description`, in the imperative, for
example `fix(api): return 404 for a missing task`. The pull request title becomes the commit on `main`, so it follows
the same format.

**Types:** `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`.
The scope is optional and names the area: `api`, `web`, `desktop`, `ui-lib`, `db`, `ci` or `deps`.

## Pull request descriptions

Write for a reader who is new to the project. The template has three sections:

- **Summary:** what changes and why.
- **Notes for Reviewers:** decisions, trade-offs and anything easy to miss. Leave it out when there is nothing to add.
- **Test Plan:** what you ran or checked, and what it showed.

## Building and testing

The API, web app and libraries build with the .NET 10 SDK on any platform. The desktop app targets Windows; elsewhere,
build it with `-r win-x64 -p:EnableWindowsTargeting=true`. See [tests/README.md](tests/README.md) to run the test suites.
The API integration tests need Docker or Podman for a SQL Server container.
