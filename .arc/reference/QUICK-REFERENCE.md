# Quick Reference - TaskFocus v2.0

**Version**: 1.0 | **Updated**: 2025-10-14 | **Location**: `.arc/reference/`

## About This Reference Directory

**Key documentation:**

- `constitution/` - Project principles (META-PRD, TECHNICAL-ARCHITECTURE, PROJECT-STATUS)
- `workflows/` - Core process guides (0-define-constitution.md, 1-create-prd.md, 2-generate-tasks.md, 3-process-task-loop.md)
- `upcoming/` - Work in progress (PRDs and Task Lists)

---

## Environment & Path Context

**Repository Root**: `/home/andrew/dev/TaskFocus/`
**All commands in this document assume you are at the repository root.**

### Critical Path Reference

| Resource | Location from Repo Root | Why It Matters |
|---|---|---|
| Solution File | `TaskFocus.sln` | Defines the project structure for .NET |
| Core Logic | `src/Core/` | Domain and Application layers |
| Infrastructure | `src/Infrastructure/` | Persistence, Identity, etc. |
| Presentation | `src/Presentation/` | Blazor Web App and API endpoints |
| Tests | `tests/` | Unit, Integration, and E2E tests |

### Network Architecture & Ports

- **Web Application**: The .NET Kestrel server hosts both the Blazor frontend and the backend API in a single process
  during development.
- **Default Ports**: Typically `https://localhost:7XXX` and `http://localhost:5XXX`. The exact ports are defined in
  `src/Presentation/TaskFocus.Web/Properties/launchSettings.json`.
- **Database**: SQL Server will run in a Docker container, accessible on port `1433`.

---

## Command Patterns

### Running the Application

```bash
# Run the web app with hot reload enabled
dotnet watch run --project src/Presentation/TaskFocus.Web
```

### Code Quality (Linting & Analysis)

In our .NET project, code formatting rules and static analysis are enforced by Roslyn Analyzers via the
`.editorconfig` file. These checks run automatically during the build.

```bash
# To check for any code quality issues, simply build the solution
dotnet build
```

### Testing

```bash
# Run all test projects in the solution
dotnet test

# Run tests in a specific project
dotnet test tests/unit/TaskFocus.Domain.Tests

# Run tests with code coverage (example)
# Note: Requires the coverlet.collector package
dotnet test --collect:"XPlat Code Coverage"
```

### Database Migrations (Entity Framework Core)

**CRITICAL**: These commands require the EF Core global tool (`dotnet tool install --global dotnet-ef`).
The `--startup-project` is necessary to find the connection string and services.

```bash
# Create a new database migration
dotnet ef migrations add [MigrationName] --project src/Infrastructure/TaskFocus.Persistence --startup-project src/Presentation/TaskFocus.Web

# Apply migrations to the database (update the schema)
dotnet ef database update --startup-project src/Presentation/TaskFocus.Web

# Remove the last migration (if something went wrong)
dotnet ef migrations remove --project src/Infrastructure/TaskFocus.Persistence --startup-project src/Presentation/TaskFocus.Web
```

### Markdown Linting

```bash
# Check for markdown issues
npx markdownlint-cli2 "README.md" ".arc/**/*.md"

# Auto-fix markdown issues
npx markdownlint-cli2 --fix "README.md" ".arc/**/*.md"
```

---

## Quality Gate Commands

Reference for `DEVELOPMENT-RULES.md` quality gates. Run these before any commit.

```bash
# 1. Build & Analyze (Builds the solution and runs Roslyn analyzers)
dotnet build

# 2. Test (Runs all xUnit test projects)
dotnet test

# 3. Markdown Linting (Checks documentation)
npx markdownlint-cli2 "README.md" ".arc/**/*.md"
```

---

## Docker Operations (for SQL Server)

```bash
# Start the SQL Server container for the first time
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=yourStrong(!)Password" \
   -p 1433:1433 --name taskfocus-sql -d \
   mcr.microsoft.com/mssql/server:2022-latest

# Check container status
docker ps | grep taskfocus-sql

# Start an existing, stopped container
docker start taskfocus-sql

# Stop the container
docker stop taskfocus-sql

# View logs in real-time
docker logs -f taskfocus-sql
```
