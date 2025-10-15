# PRD: Foundational Slice & CI/CD

## 1. Introduction

This document defines the requirements for the initial foundational milestone of the TaskFocus v2.0 project.  
The goal is not to deliver a user-facing feature, but to build and validate the core end-to-end architecture  
and development loop. This involves scaffolding the solution, implementing a single "vertical slice" for  
creating a task, and establishing a continuous integration (CI) pipeline.

This work directly supports the vision in `META-PRD.md` by laying the technical groundwork for all future features.

## 2. Goals

- To create and validate the .NET 8 solution structure based on Onion and Vertical Slice architecture.
- To implement a single, working end-to-end feature ("Create Task") to serve as a pattern for future development.
- To establish an automated CI pipeline in GitHub Actions that builds and tests the code on every push.
- To confirm the viability of the core technology choices (EF Core, MediatR, Minimal APIs, Blazor, xUnit).

## 3. User Stories

As a developer:

- I want a correctly scaffolded solution so that I can immediately begin implementing features  
  within the correct architectural layers.
- I want a simple, working end-to-end feature slice so that I have a clear, repeatable pattern to follow  
  for all subsequent features.
- I want an automated CI pipeline so that I have high confidence that my changes have not broken the build or existing tests.

## 4. Functional Requirements

### API & Backend

1. The system must provide a Minimal API endpoint: `POST /api/tasks`.
2. The endpoint must accept a request body containing at least a `title` for the new task.
3. The endpoint handler must use a MediatR command to process the request.
4. The command handler must persist a new `Task` entity to the SQL Server database using EF Core.
5. The `Task` entity must include at a minimum: `Id`, `Title`, `DateCreated`.

### Web Frontend

1. The system must provide a basic Blazor page containing a text input for the task title and a "Create" button.
2. Clicking the "Create" button must call the `POST /api/tasks` endpoint with the provided title.
3. The UI must provide a simple visual confirmation that the operation succeeded or failed.

### CI/CD

1. A GitHub Actions workflow must be created.
2. The workflow must be triggered on every `push` to any feature branch and the `main` branch.
3. The workflow must successfully restore dependencies, build the entire solution (`dotnet build`),  
   and run all tests (`dotnet test`).
4. A failure in any step (build or test) must fail the workflow.

## 5. Non-Goals (Out of Scope)

- **Authentication/Authorization**: The API endpoint will be anonymous for this milestone.
- **Complex UI**: The Blazor UI will be functionally minimal. No advanced styling, componentization,  
  or state management (Fluxor) will be implemented yet.
- **Deployment (CD)**: The CI pipeline will only build and test; it will not deploy the application to Azure.
- **Full CRUD**: Reading, updating, or deleting tasks is not in scope.
- **Input Validation**: Advanced input validation (e.g., FluentValidation) is not required for this slice.

## 6. Technical Considerations

- All implementation must adhere to the patterns and technologies defined in `TECHNICAL-ARCHITECTURE.md`.
- The initial database schema will be created using EF Core migrations.

## 7. Success Metrics

- A successful, green run of the GitHub Actions CI pipeline.
- A new task can be created via the Blazor UI and successfully persisted, as verified by inspecting the database directly.
- The created solution structure correctly reflects the Onion/Vertical Slice architecture.
