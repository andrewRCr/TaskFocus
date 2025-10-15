# TaskFocus v2.0 - Shared AI Context

## Project Overview

TaskFocus v2.0 is a complete modernization of a personal task management application, rebuilt from the ground up  
to serve as a portfolio project demonstrating best practices in modern .NET and web development.

**Project Type**: Solo portfolio project for skill demonstration
**Primary Goal**: Showcase clean, maintainable, performant, and well-tested application  
built with modern Microsoft-centric tech stack
**Target Users**: Technical evaluators (hiring managers, senior engineers)  
and individuals seeking a personal productivity tool

## ARC Framework Integration

### 📋 Key Reference Documents

**Constitutional Documents** (Core project foundation):

- [META-PRD](../constitution/META-PRD.md) - Product vision, core features, user flows, and success metrics
- [PROJECT-STATUS](../constitution/PROJECT-STATUS.md) - Current progress, completed work, and upcoming priorities  
- [TECHNICAL-ARCHITECTURE](../constitution/TECHNICAL-ARCHITECTURE.md) - Technical architecture, patterns,  
  and implementation details
- [DEVELOPMENT-RULES](../constitution/DEVELOPMENT-RULES.md) - Development standards, quality gates,  
  and AI collaboration protocols
- [QUICK-REFERENCE](../QUICK-REFERENCE.md) - Environment context, command patterns, and tool usage

### 🔄 Development Workflow

**ARC Framework Core Workflows** (Systematic approach for feature development):

1. [Define Constitution](../workflows/0-define-constitution.md) - Project setup and constitutional document creation
2. [Create PRD](../workflows/1-create-prd.md) - Generate feature-level PRDs for new features
3. [Generate Tasks](../workflows/2-generate-tasks.md) - Create implementation task lists from PRDs
4. [Process Task Loop](../workflows/3-process-task-loop.md) - Task execution and completion protocols

**Supplemental Workflows** (Supporting processes):

- [Session Handoff](../workflows/supplemental/session-handoff.md) - Context preservation across sessions
- [Atomic Commit](../workflows/supplemental/atomic-commit.md) - Structured commit protocols
- [Manage Incidental Work](../workflows/supplemental/manage-incidental-work.md) - Handle reactive maintenance tasks
- [Archive Completed](../workflows/supplemental/archive-completed.md) - Clean up finished work

### 📄 Generated Deliverables

**Feature Development**:

- [PRDs](../../upcoming/prds/) - Feature-level specifications generated from workflow
- [Tasks](../../active/) and [Upcoming Tasks](../../upcoming/tasks/) - Implementation task lists generated from PRDs
- [Active Work](../../active/) - Current feature development and incidental work

**Knowledge Management**:

- [Strategies](../strategies/) - Evolved patterns and architectural decisions
- [Archive](../archive/) - Completed work and historical context

## Project-Specific Context

### Technology Stack

**Backend**:

- **Framework**: ASP.NET Core 8 (.NET 8)
- **Language**: C# 12
- **API Design**: Minimal APIs organized by feature into Vertical Slices
- **Logic Orchestration**: MediatR for implementing CQRS pattern
- **Validation**: FluentValidation integrated into MediatR pipeline
- **Database**: Entity Framework Core 8 with SQL Server (Code-First approach)
- **Authentication**: ASP.NET Core Identity with JWT tokens

**Frontend**:

- **Framework**: Blazor Web App (.NET 8) using interactive render modes (SSR + WASM)
- **Language**: C# 12
- **UI Components**: Microsoft Fluent UI Blazor
- **State Management**: Fluxor (Redux pattern for .NET)

**Infrastructure**:

- **Containerization**: Docker and Docker Compose for local SQL Server
- **CI/CD**: GitHub Actions
- **Hosting**: Azure Container Apps for production
- **Database**: Azure SQL Database for production
- **Infrastructure-as-Code**: Terraform for Azure resources
- **Monitoring**: Azure Monitor and Application Insights

### Development Environment

- **Local Development**: VS Code within WSL (Windows Subsystem for Linux)
- **Repository Root**: `/home/andrew/dev/TaskFocus/`
- **Solution File**: `TaskFocus.sln` in repository root
- **Testing Framework**: xUnit with FluentAssertions, NSubstitute for mocks
- **Integration Tests**: Testcontainers for real SQL Server instances
- **E2E Testing**: Playwright for browser automation
- **Code Quality**: EditorConfig + Roslyn analyzers, markdownlint for documentation
- **Development Workflow**: Feature branches, Docker for dependencies, automated quality gates

### Key Architectural Patterns

**Architecture**: Onion Architecture combined with Vertical Slice Architecture

- **Core Layer**: Domain entities and Application use cases (CQRS)
- **Infrastructure Layer**: Data persistence, identity, external services
- **Presentation Layer**: Blazor Web App and Minimal API endpoints

**Authentication**: ASP.NET Core Identity with JWT token-based API authentication
**Error Handling**: Centralized exception handling with structured logging
**State Management**: Fluxor for predictable state updates across Blazor components
**API Design**: RESTful endpoints using Minimal APIs with OpenAPI documentation
**Data Access**: Repository pattern with EF Core, Code-First migrations

### Development Standards

**ARC Framework Standards** (Applied to TaskFocus v2.0):

- **Code Quality**: EditorConfig + Roslyn analyzers, zero tolerance for warnings/errors
- **Testing**: Required before commits, >80% coverage target for backend
- **Feature Branches**: All feature work on dedicated branches (`feature/[name]`)
- **Commits**: Conventional commit format, never without explicit user approval
- **Documentation**: Comprehensive markdown docs, architectural decision records
- **AI Collaboration**: Follow protocols in DEVELOPMENT-RULES.md v1.1
- **Quality Gates**: Build + Tests + Linting must pass before any commit

### User-Focused Features

**Core Task Management**:

- **Task CRUD**: Create, read, update, delete tasks with due dates and completion status
- **Organization**: Assign tasks to Projects and Contexts for better organization
- **Core Views**: Inbox (unorganized), Today (due today), Projects, Contexts, Completed

**User & Identity**:

- **Registration/Login**: Secure user accounts with JWT authentication
- **Single User Focus**: Designed for individual productivity, not collaboration

**Future Enhancements** (Stretch Goals):

- **Real-Time Sync**: SignalR for live updates across browser tabs
- **Natural Language Input**: LLM integration for parsing "Call John tomorrow at 5pm"
- **Calendar View**: Month view with enhanced date properties

## ARC Framework Development Workflow Integration

### Standard Development Process

When working on new features:

1. **Start with context**: Review META-PRD for product alignment
2. **Use systematic approach**: Follow the 4-step ARC workflow:
   - Define/update constitutional documents as needed
   - Create feature-level PRD → generate tasks → process task loop
3. **Work on feature branches**: All feature development happens on dedicated branches
4. **Maintain quality**: Full testing and quality gates before any commits
5. **Follow AI protocols**: Session management, commit approval, task synchronization
6. **Seek explicit approval**: Never commit without user review and permission

### AI Collaboration Protocols

**Session Management**:

- Review CURRENT-SESSION.md at start of work
- Follow session handoff protocols for context preservation  
- NEVER update CURRENT-SESSION.md without explicit user instruction

**Task Execution**:

- Complete ONE sub-task at a time
- Wait for explicit approval between sub-tasks
- Update task documentation immediately after each sub-task
- Perform comprehensive task context analysis before commits

**Quality Standards**:

- All quality gates must pass before commits (see DEVELOPMENT-RULES.md)
- Leave code cleaner than found (pre-existing issue protocol)
- Report issues immediately with full context

**AI Session Initialization Required**:

Before starting ANY work:

1. **Confirm rules version**: "Acknowledging DEVELOPMENT-RULES v{x}, hash {x}"
2. **Verify environment**: Check working directory (`/home/andrew/dev/TaskFocus/`), Docker status
3. **State understanding**: Confirm commit protocols, task analysis, quality gates
4. **Verify context access**: Confirm access to QUICK-REFERENCE, AI-SHARED, META-PRD, etc.
5. **Check branch status**: Verify current git branch and alignment with intended work

## Document Dependencies

### When Constitutional Documents Change

**META-PRD.md changes** → Update:

- `ai-instructions/AI-SHARED.md` (this file) - project overview and features
- Potentially `PROJECT-STATUS.md` - if scope or priorities change

**DEVELOPMENT-RULES.md changes** → Update:

- Version number in DEVELOPMENT-RULES.md
- All `ai-instructions/*.md` files - if protocols change
- Team communication about rule changes

**TECHNICAL-ARCHITECTURE.md changes** → Update:

- `ai-instructions/AI-SHARED.md` (this file) - technology stack and patterns
- Consider PROJECT-STATUS.md if architectural decisions affect roadmap

**PROJECT-STATUS.md changes** → Update:

- Consider if major status changes affect ongoing work priorities

## Quick Information Lookup

### "How do I...?"

**ARC Framework Processes**:

- **Set up project foundation** → `workflows/0-define-constitution.md`
- **Start a new feature** → `workflows/1-create-prd.md`
- **Break down tasks** → `workflows/2-generate-tasks.md`  
- **Implement tasks** → `workflows/3-process-task-loop.md`
- **Handle maintenance work** → `workflows/supplemental/manage-incidental-work.md`
- **Hand off session** → `workflows/supplemental/session-handoff.md`

**Project Information**:

- **Run tests/linting** → `QUICK-REFERENCE.md` (Quality Gate Commands)
- **Understand the product** → `META-PRD.md`
- **See current progress** → `PROJECT-STATUS.md`
- **Learn the architecture** → `TECHNICAL-ARCHITECTURE.md`
- **Environment setup** → `QUICK-REFERENCE.md` (Environment & Path Context)

### "What are the rules for...?"

- **Development standards** → `DEVELOPMENT-RULES.md`
- **AI collaboration protocols** → `DEVELOPMENT-RULES.md` (AI Session sections)
- **Code quality requirements** → `TECHNICAL-ARCHITECTURE.md` + `DEVELOPMENT-RULES.md`
- **Commit format and process** → `workflows/supplemental/atomic-commit.md`
- **Task management** → `workflows/3-process-task-loop.md`

### "What commands do I run for...?"

- **Build the solution** → `dotnet build` (from repo root)
- **Run tests** → `dotnet test` (from repo root)
- **Run the app** → `dotnet watch run --project src/Presentation/TaskFocus.Web`
- **Check markdown** → `npx markdownlint-cli2 "README.md" ".arc/**/*.md"`
- **Database migrations** → See QUICK-REFERENCE.md (Database Migrations section)
- **Docker SQL Server** → See QUICK-REFERENCE.md (Docker Operations section)

## Project Goals and Context

**Primary Objectives** (See META-PRD.md for details):

- Demonstrate modern .NET and web development best practices
- Showcase clean architecture, comprehensive testing, and CI/CD pipelines
- Create a polished, functional task management application

**Key Principles** (See META-PRD.md for rationale):

- Clean, maintainable code with Onion + Vertical Slice Architecture
- Comprehensive testing with >80% backend coverage
- Professional documentation and architectural decision records
- Modern development practices with automated quality gates

**Success Metrics Summary** (See META-PRD.md for complete metrics):

- Code quality with clean static analysis results
- High test coverage and CI/CD automation
- Polished UX with responsive design and performance optimization

**For complete project vision, user flows, detailed objectives, and success criteria, refer to [META-PRD.md](../constitution/META-PRD.md).**

---

*This document is maintained as part of the ARC (Agentic, Recursive, Coordination) development framework.
It provides essential context for both human developers and AI assistants working on this project.*
