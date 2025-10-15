# TaskFocus v2.0 - Tech Stack & Architecture

## Purpose

This document captures the agreed-upon high-level architecture and technology stack for the TaskFocus v2.0
modernization effort.
It serves as a reference for all subsequent development.

## High-Level Strategy

- **Phased Approach:** Development will occur in phases.
  - **Phase 1: The Modern Core:** Focus exclusively on building a server-authoritative backend with a responsive web frontend.
    This includes the API, domain logic, data persistence, testing, and CI/CD.
  - **Phase 2: Client Expansion:** A desktop client (e.g., .NET MAUI) will only be considered after the core web
    application is stable and complete. The WPF client is retired.
- **Web-First:** The primary client will be a modern, responsive web application.
  A PWA will be considered for an "installable" experience.
- **Clean Database:** The project will start with a new, clean database schema. No data migration from v1.0 will be performed.

## Technology Choices

- **Development Environment:**
  - **OS/Shell:** WSL (Windows Subsystem for Linux)
  - **Editor:** VS Code
  - **Local Secrets:** `.NET user-secrets` tool.

- **Core Architecture:**
  - **Pattern:** Onion Architecture combined with **Vertical Slice Architecture**.
  - **Logic Orchestration:** **MediatR** to implement the CQRS pattern within slices.

- **Backend:**
  - **Framework:** **ASP.NET Core Minimal APIs**.
  - **Data Access:** **Entity Framework Core** (Code-First).
  - **Validation:** **FluentValidation** integrated into the MediatR pipeline.

- **Database:**
  - **Engine:** **Microsoft SQL Server** (run via Docker for local development).
  - **Schema Management:** EF Core Migrations.

- **Frontend:**
  - **Framework:** **Blazor Web App** (.NET 8) leveraging mixed/interactive render modes (SSR, WASM, etc.).
  - **Component Library:** **Microsoft Fluent UI Blazor**.
  - **State Management:** **Fluxor**.

- **Testing:**
  - **Test Runner:** **xUnit**.
  - **Assertion Library:** **FluentAssertions**.
  - **Mocking:** **NSubstitute**.
  - **Integration Testing:** **Testcontainers** (to run SQL Server in Docker during tests).
  - **End-to-End (E2E) Testing:** **Playwright**.

- **DevOps & Infrastructure:**
  - **CI/CD:** **GitHub Actions**.
  - **Containerization:** **Docker**.
  - **Infrastructure-as-Code (IaC):** **Terraform**.
  - **Deployment Target:** **Azure Container Apps** with **Azure SQL Database**.
