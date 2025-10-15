# TaskFocus Modernization Discovery Notes

## Purpose

- Build a shared picture of the current TaskFocus 1.0 implementation and the blank-slate 2.0 scaffolding.
- Capture early observations, risks, and modernization opportunities before we commit to scope.
- Identify open questions and discovery work needed to validate architecture and delivery decisions.

## Repository Baseline (July 2024)

- `legacy/` holds the entire 1.0 codebase:
  - API
  - shared UI library
  - WPF desktop client
  - Blazor WASM client
  - SQL database project
  - Azure DevOps pipeline assets
- `src/` reflects a fresh onion/clean architecture layout (`Core`, `Infrastructure`,
  `Presentation`) but projects are placeholders; solution `TaskFocus.sln` includes only
  `TaskFocus.Core.Test` today.
- `tests/` already exposes target directories for `unit`, `integration`, and `e2e`
  suites but contains no implementations yet.
- `.arc/` has documentation scaffolding; active feature folders are empty until we
  populate them with this modernization effort.

## Legacy 1.0 Findings

### Architecture & Code Organization

- Single `legacy/TaskFocus.sln` stitches together API, UI clients, shared UI
  library, and SQL project; cross-layer dependencies flow freely without domain
  boundaries.
- API controllers depend directly on `TaskFocusAPI.Library` (netstandard2.0),
  which mixes DTOs, data-access, and utility logic; there is no domain-centric
  abstraction to isolate business rules.
- UI clients consume `TaskFocusUI.Library`, which centralizes API access, data
  state, synchronization, logging, and models for both WPF and Blazor, creating
  tight coupling between presentation layers and backend DTOs.
- Dependency injection is configured manually (Caliburn SimpleContainer, direct
  `AddTransient` registrations) and leans on concrete types rather than contracts
  expressed per layer.

### Domain & Data Modeling

- Business models (`TaskFocusAPI.Library/Models`) mirror database tables;
  application behaviour (cleanup rules, sync metadata) lives primarily inside
  client-side services instead of the API/domain.
- Data access uses Dapper + stored procedures via `SqlDataAccess`; logic is
  transaction-heavy and leans on raw SQL strings, undermining testability and
  migration flexibility.
- Identity/Auth data uses EF Core in the API, creating two parallel persistence
  stacks (EF and Dapper) with duplicated configuration and transactional rules.
- SQL project (`TaskFocusData.sqlproj`) targets SQL Azure with stored procedures
  as the contract; no code-first migrations or automated schema drift detection.
- Concurrency is handled manually through timestamp columns and client-side
  conflict handling; no optimistic concurrency checks at the data layer.

### Backend/API Implementation

- ASP.NET Core 8 MVC application exposes controller actions returning internal
  models directly; there is no DTO mapping, validation pipeline, or
  ProblemDetails response standardization.
- `TokenController` issues JWTs through manual Key Vault lookups on each request;
  there is no caching, throttling, or refresh token support, and secrets are fetched synchronously in the request path.
- REST semantics are inconsistent (`TaskController.Delete` accepts a body,
  endpoints expose user-specific data via query path without explicit
  authorization policies/data shaping).
- No API versioning, health checks, rate limiting, request logging, or structured monitoring is in place.
- Swagger is configured but exposed at the root in production; there is no
  separation of public/private endpoints or security hardening around metadata.

### Web Client (Blazor WASM)

- `TaskFocusWeb` targets .NET 8 WASM with MudBlazor but relies on a hand-rolled
  `AppState` eventing mechanism and the shared `TaskFocusUI.Library` for almost
  all logic.
- HTTP calls use raw `HttpClient` instances created in a helper without
  `IHttpClientFactory`, resiliency policies, or typed clients.
- State management, caching, and sync logic are tightly coupled to UI components,
  complicating testability and potential reuse in other clients.
- Client-side validation, performance optimizations (virtualization, lazy
  loading), and accessibility support appear minimal.
- No automated UI or component tests exist; manual regression is the only safety net.

### Desktop Client (WPF + Caliburn Micro)

- Caliburn SimpleContainer registers most services as singletons and relies on
  convention-based view model wiring; threading and lifetime concerns are
  hand-managed.
- Heavy use of global state objects (`IDataState`, `IAppState`, `ILoggedInUserModel`) stored as singletons increases
  coupling and makes automated testing difficult.
- Custom logging implementation writes to in-memory collections and MessageBox
  prompts; there is no pipeline to forward logs or metrics to centralized
  observability tooling.
- Packaging uses `PublishSingleFile` self-contained builds; no installer, code
  signing, or update mechanism is described.

### Synchronization & State Handling

- `DataSyncService` orchestrates periodic sync with manual retry loops, global
  flags, and in-memory staging lists; there is no cancellation token management
  or resilient background processing infrastructure.
- Sync rules (cleanup, today view assignment, index recalculation) run client-side
  before pushes, so business invariants depend on front-end behaviour instead of
  authoritative backend rules.
- Conflict resolution is timestamp-based (`LastUpdated`) with no audit trail,
  telemetry, or reconciliation workflow for end users.
- Offline support remains rudimentary (in-memory only); there is no durable local
  storage strategy for desktop or web beyond immediate session state.

### Quality & Testing

- Aside from template `Class1` in `TaskFocus.Core.Test`, there are no unit,
  integration, or end-to-end tests across API, synchronization logic, or UI.
- Manual QA is the only safety net; no linting, code coverage tracking, or static analysis integration is apparent.

### DevOps, Delivery, and Infrastructure

- Azure DevOps pipeline (`legacy/standard-CI-CD-pipeline.yml`) builds all
  artifacts on Windows agents, performs token replacement on JSON, publishes ZIP
  bundles, and deploys directly to Azure Web Apps and SQL Azure via DacPac.
- There is no infrastructure-as-code beyond the SQL project; environment
  provisioning, secrets, and app configuration appear to be handled manually.
- Pipeline installs global `dotnet-ef` and runs migrations ad hoc; there is no
  automated rollback or schema drift detection.
- Testing stages are absent; deployments occur immediately after build without gated validations.

### Security & Compliance

- API and clients pull secrets from Key Vault but also expect plaintext configuration files beside executables;
  secure configuration management is inconsistent.
- No mention of security headers, CSP, data encryption at rest, or GDPR/PII handling; logging likely captures
  user identifiers without masking.
- Authentication/authorization responsibilities are mostly server-side Identity roles; clients store JWT tokens
  in memory/local storage without rotation.

### Documentation & Developer Experience

- README describes 1.0 features but not architecture decisions, tech debt, or environment setup.
- `.arc` scaffolding is present but unused; there is no living documentation of
  sync algorithms, data contracts, or deployment processes.
- Solution uses `global.json` to pin SDK 8.0.120 but there is no documented
  workflow for upgrades or local tooling requirements.

## Initial Modernization Considerations

### Architecture Goals

- Flesh out the onion/clean architecture skeleton: `Domain` for business
  entities/invariants, `Application` for use cases (CQRS/MediatR?),
  `Infrastructure` for persistence/integration, `Presentation` for API/clients.
- Establish clear interface boundaries and dependency rules (Domain <-
  Application <- Infrastructure/Presentation) enforced via solution structure
  and analyzers.
- Consider modular monolith vs microservice split; given scope, a modular monolith with vertical slices may
  balance complexity and maintainability.

### Data & Persistence Strategy

- Decide whether to migrate away from stored procedures toward EF Core code-first migrations or adopt a hybrid
  (EF for orchestration, stored procedures for hot paths).
- Introduce explicit aggregates/value objects to centralize cleanup, task
  indexing, and GTD rules on the server.
- Evaluate temporal tables or change tracking for sync, possibly leveraging EF
  Core concurrency tokens instead of manual timestamps.
- Plan data migration from 1.0 schema to 2.0 (backfill scripts, dual-write strategy, or migration downtime).

### API & Integration Layer

- Choose API style (Minimal API vs Controllers) and standardize on DTOs + AutoMapper/Mapster as needed.
- Incorporate validation (`FluentValidation`), exception handling middleware,
  API versioning, and ProblemDetails responses.
- Adopt `IHttpClientFactory`, Polly policies, and typed clients for outbound
  calls (email, key vault, third-party integrations).
- Evaluate using Identity + OpenIddict / Azure AD B2C for auth vs maintaining
  custom token endpoints; plan for refresh tokens and short-lived access tokens.
- Introduce background services or message queues (Azure Service Bus?) if sync moves server-side.

### Client Applications

- Web: confirm commitment to Blazor WASM vs Blazor Server/Hybrid vs another SPA framework; assess readiness to
  adopt Fluxor/Redux-style state management, query libraries, and component test tooling.
- Desktop: decide whether to retire WPF or re-platform to .NET MAUI/Blazor Hybrid; consider delaying desktop
  overhaul until backend/web stabilize.
- Shared client logic should move toward reusable contracts/services that consume the new API rather than
  embedding business rules client-side.

### Synchronization & Offline Story

- Reassess whether true bi-directional sync is still required; if yes, design authoritative server logic with
  eventual consistency and conflict resolution strategies.
- Investigate server-driven push (SignalR, WebSockets) or background sync endpoints vs polling.
- For offline support, plan durable local storage (SQLite on desktop, IndexedDB for web) with a formal sync
  protocol and schema versioning.

### Observability & Diagnostics

- Standardize on `Serilog` (or another provider) with structured logging sinks (Seq, Application Insights) and
  enrichers for user/request context.
- Integrate OpenTelemetry for distributed tracing and metrics; add health checks, readiness/liveness probes
  for container deployments.
- Define log retention, PII handling, and alerting policies early to avoid gaps in production.

### Quality Engineering

- Design a test strategy aligned with the new architecture: domain unit tests,
  application/service integration tests (with EF Core in-memory/Testcontainers),
  API contract tests, and UI smoke/E2E tests (Playwright/MSTest).
- Incorporate static analysis (Roslyn analyzers, StyleCop, Sonar), code coverage thresholds, and pre-commit hooks.
- Consider consumer-driven contracts between API and clients to keep shared contracts explicit.

### DevOps & Deployment

- Transition CI/CD to GitHub Actions with environment-specific workflows (build, test, security scans, deploy).
- Containerize API (and potentially Blazor) using Docker; evaluate Azure Container Apps, Azure Web App for Containers,
  or Kubernetes depending on scope.
- Manage infrastructure via Bicep/Terraform, and externalize configuration through environment variables +
  Azure Key Vault/Managed Identity.
- Implement promotion workflows (dev -> staging -> production) with approvals and smoke tests.

### Security & Governance

- Remove any plaintext secrets from the repo (if present); enforce secret scanning (GitHub
  Advanced Security, truffleHog) and add commit hooks.
- Adopt centralized identity (Azure AD, Entra External ID, Duende) with refresh
  token rotation, MFA, and role/claim management.
- Implement secure headers, CSRF/anti-forgery mitigations, and OWASP ASVS-aligned controls.
- Evaluate data privacy requirements (logging scrubbing, data retention) before
  storing additional telemetry.

### Documentation & Knowledge Management

- Populate `.arc` constitution/workflows to capture architecture decisions
  (ADRs), domain glossary, and modernization roadmap.
- Maintain a running changelog and migration playbooks to aid portfolio
  presentation and onboarding.
- Document developer setup for both legacy reference and the new stack to reduce ramp-up friction.

## Open Questions / Research Needed

- What portions of the legacy domain rules (cleanup, indexing, GTD workflows) must remain identical vs redesigned?
- Are there existing production users whose data must migrate seamlessly, or can 2.0 start with a clean database?
- What service-level objectives (availability, performance, sync latency) should 2.0 meet?
- Do we need continued WPF support long term, or can MAUI/Blazor Hybrid replace it after the backend rebuild?
- Which external services (email, notifications, identity providers) are mandatory for 2.0 launch,
  and are there licensing constraints?
- How will secrets and environment configuration be managed locally in development without reintroducing plaintext files?

## Suggested Immediate Discovery Tasks

- Audit the `legacy/Database` project and stored procedures to document domain invariants before re-implementing them server-side.
- Catalog API surface area and payloads from 1.0 to inform contract design and migration tooling.
- Inventory shared models/services in `TaskFocusUI.Library` to determine what logic must migrate to the server
  vs remain client-side.
- Prototype the onion architecture solution structure (projects, shared abstractions, dependency analyzers) to
  validate the folder layout.
- Evaluate tooling for new pipelines (GitHub Actions templates, test harnesses, static analysis) and document required secrets/integrations.
- Draft initial modernization ADRs covering architecture vision, persistence strategy, and client technology
  decisions before implementation begins.
