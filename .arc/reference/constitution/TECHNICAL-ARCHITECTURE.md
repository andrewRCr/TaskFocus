# TaskFocus v2.0 Technical Architecture

This document outlines the technical architecture of TaskFocus v2.0, covering the implementation stack,
development practices, and operational considerations.

<!--
- Document technical decisions and architectural patterns
- Keep this focused on implementation details, not product vision
- Update as architecture evolves
-->

## 1. Architecture Overview

TaskFocus v2.0 is a modern full-stack web application built on .NET 8. The architecture follows the
principles of **Onion Architecture** to ensure a separation of concerns, combined with
**Vertical Slice Architecture** for implementing business use cases. This creates a modular,
maintainable, and testable system.

- **Core**: Contains the `Domain` (entities) and `Application` (use cases/CQRS) layers.
- **Infrastructure**: Implements external concerns like data persistence, identity, and external services.
- **Presentation**: The user-facing Blazor web application and the web API.

## 2. Backend Architecture

### Backend Technology Stack

- **Framework**: ASP.NET Core 8
- **Language**: C# 12
- **API Design**: **Minimal APIs** organized by feature into Vertical Slices.
- **Logic Orchestration**: **MediatR** for implementing the CQRS pattern.
- **Validation**: **FluentValidation** integrated into the MediatR pipeline.

## 3. Frontend Architecture

### Frontend Technology Stack

- **Framework**: **Blazor Web App** (.NET 8) using interactive render modes (e.g., SSR + WASM).
- **Language**: C# 12
- **UI Components**: **Microsoft Fluent UI Blazor**.
- **State Management**: **Fluxor** (a .NET implementation of the Flux/Redux pattern).

## 4. Data Architecture

### Database Design

- **Primary Database**: **Microsoft SQL Server**.
- **Data Modeling**: **Entity Framework Core 8** using a **Code-First** approach.
- **Migrations**: Database schema changes are managed via EF Core Migrations.

## 5. Infrastructure & Deployment

### Development Environment

- **Containerization**: **Docker** and Docker Compose for running local dependencies (e.g., SQL Server).
- **Local Development**: VS Code within WSL (Windows Subsystem for Linux).
- **Environment Configuration**: `.NET user-secrets` for local development secrets.

### CI/CD Pipeline

- **Continuous Integration**: **GitHub Actions**.
- **Quality Gates**: Automated build, test, and code analysis.
- **Deployment Process**: Automated deployments triggered by merges to the main branch.

### Production Architecture

- **Hosting**: **Azure Container Apps** for the backend API and Blazor application.
- **Database**: **Azure SQL Database**.
- **Infrastructure-as-Code**: **Terraform** to define and manage all Azure resources.
- **Monitoring**: **Azure Monitor** and **Application Insights** for logging and telemetry.

## 6. Testing Strategy

- **Backend Testing**:
  - **Framework**: **xUnit**.
  - **Assertions**: **FluentAssertions**.
  - **Mocks**: **NSubstitute**.
  - **Integration Tests**: **Testcontainers** to spin up a real SQL Server instance in Docker for tests.
- **Frontend Testing**:
  - **Framework**: **bUnit** for component testing.
- **E2E Testing**: **Playwright** for end-to-end browser tests.

---

*This document will be updated as the technical foundation evolves.*
