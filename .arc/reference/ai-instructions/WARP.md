# TaskFocus v2.0 - WARP Terminal Guidance

**Context**: This document provides terminal-focused guidance for TaskFocus v2.0 development within WARP terminal.  
For comprehensive project context, refer to [AI-SHARED.md](./AI-SHARED.md).

## Environment Context

**Current Working Directory**: `/home/andrew/dev/TaskFocus/`
**Shell**: bash 5.2.21(1)-release  
**Platform**: Linux (Ubuntu) via WSL
**Editor**: VS Code within WSL

## TaskFocus v2.0 Specific Commands

### Project Structure Commands

```bash
# Navigate to key project areas
cd src/Core/TaskFocus.Domain          # Domain entities
cd src/Core/TaskFocus.Application     # CQRS handlers
cd src/Infrastructure/TaskFocus.Persistence  # EF Core data layer
cd src/Presentation/TaskFocus.Web     # Blazor app + API
cd tests/                             # All test projects

# Quick project structure overview
find src/ -name "*.csproj" -type f | head -10
```

### Development Workflow

```bash
# Full quality gate check (run from repo root)
dotnet build && dotnet test && npx markdownlint-cli2 "README.md" ".arc/**/*.md"

# Watch mode for active development
dotnet watch run --project src/Presentation/TaskFocus.Web

# Clean and rebuild everything
dotnet clean && dotnet build

# Run specific test project
dotnet test tests/unit/TaskFocus.Domain.Tests --logger console
```

### Database Operations (Dockerized)

```bash
# Start SQL Server container (first time)
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=yourStrong(!)Password" \
   -p 1433:1433 --name taskfocus-sql -d \
   mcr.microsoft.com/mssql/server:2022-latest

# Start existing container
docker start taskfocus-sql

# Check database container status
docker ps | grep taskfocus-sql

# EF Core migrations (requires Docker SQL Server running)
dotnet ef migrations add [MigrationName] \
   --project src/Infrastructure/TaskFocus.Persistence \
   --startup-project src/Presentation/TaskFocus.Web

dotnet ef database update --startup-project src/Presentation/TaskFocus.Web
```

### Git & Branch Management

```bash
# Create and switch to new feature branch
git checkout -b feature/[feature-name]

# Check branch status and commits ahead
git status && git log --oneline -5

# Stage changes selectively (avoiding .vscode/)
git add src/ tests/ .arc/ *.md *.json *.sln

# Atomic commit with ARC workflow format
git commit -m "[type]: [brief description]

- [detailed change 1]
- [detailed change 2]
- [impact/rationale]

[Reference to PRD/task context]"
```

### Code Quality & Analysis

```bash
# Roslyn analyzer check (part of build)
dotnet build --verbosity normal

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Check solution structure
dotnet sln list

# Format check (if dotnet-format is available)
dotnet format --verify-no-changes TaskFocus.sln
```

### Docker Container Management

```bash
# View SQL Server logs
docker logs -f taskfocus-sql

# Stop SQL Server container
docker stop taskfocus-sql

# Remove container (if needed)
docker rm taskfocus-sql

# Check Docker resources
docker system df
```

### ARC Framework Integration

```bash
# Navigate to ARC documentation
cd .arc/reference/constitution/     # Core project documents
cd .arc/reference/workflows/        # Development processes
cd .arc/active/                     # Current work
cd .arc/upcoming/                   # Planned work

# Quick document lookup
find .arc/ -name "*.md" -type f | grep -E "(META-PRD|TECHNICAL|STATUS)" | head -5

# Check current session context (if exists)
ls -la .arc/active/CURRENT-SESSION.md 2>/dev/null || echo "No active session file"
```

### Performance & Monitoring

```bash
# Check application performance during development
curl -I http://localhost:5000 || echo "App not running"

# Monitor file changes
watch -n 2 'find src/ -name "*.cs" -newer /tmp/lastcheck 2>/dev/null | wc -l'

# Quick size check of key directories
du -sh src/ tests/ .arc/ | sort -hr
```

### Troubleshooting Commands

```bash
# Clear all build artifacts
find . -name "bin" -type d -exec rm -rf {} + 2>/dev/null
find . -name "obj" -type d -exec rm -rf {} + 2>/dev/null

# Check .NET SDK version
dotnet --version

# Verify required tools
which docker && which git && which code

# Check port availability
ss -tlnp | grep -E ':(1433|5000|5001|7000)'

# Environment variable check
echo "PATH includes: $(echo $PATH | tr ':' '\n' | grep -E '(dotnet|docker|node)' | head -3)"
```

## Terminal Optimization for TaskFocus v2.0

### Aliases (Add to ~/.bashrc)

```bash
# TaskFocus specific aliases
alias tf-build='cd /home/andrew/dev/TaskFocus && dotnet build'
alias tf-test='cd /home/andrew/dev/TaskFocus && dotnet test'
alias tf-run='cd /home/andrew/dev/TaskFocus && dotnet watch run --project src/Presentation/TaskFocus.Web'
alias tf-db-start='docker start taskfocus-sql'
alias tf-db-logs='docker logs -f taskfocus-sql'
alias tf-quality='cd /home/andrew/dev/TaskFocus && dotnet build && dotnet test && npx markdownlint-cli2 "README.md" ".arc/**/*.md"'
```

### Non-Interactive Patterns

```bash
# Batch operations without prompts
git add . --force
echo "y" | dotnet tool update --global dotnet-ef
docker system prune -f --volumes

# Output redirection for logging
dotnet test 2>&1 | tee test-results.log
dotnet build > build.log 2>&1

# Background processes
nohup dotnet watch run --project src/Presentation/TaskFocus.Web > app.log 2>&1 &
```

### WARP-Specific Features

```bash
# Use WARP's intelligent command suggestions
# Type "dotnet" and use Tab for context-aware completions

# Leverage WARP's directory jumping
# Use Cmd+G to quickly navigate to frequently accessed directories

# Take advantage of WARP's AI command suggestions
# Use natural language prompts in terminal for complex operations
```

## Integration with AI-SHARED Context

**For comprehensive project information, always refer to [AI-SHARED.md](./AI-SHARED.md):**

- Project overview and architecture details
- Complete technology stack specifications  
- ARC framework workflow integration
- AI collaboration protocols and quality standards
- Development environment setup and standards

**This WARP document focuses solely on terminal commands and workflow optimization for the TaskFocus v2.0 development environment.**

---

*Terminal commands optimized for TaskFocus v2.0 development in WARP terminal within WSL environment.*
