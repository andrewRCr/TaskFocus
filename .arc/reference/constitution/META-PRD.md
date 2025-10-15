# TaskFocus v2.0 Meta Product Requirements Document (META-PRD)

## 1. Purpose

TaskFocus v2.0 is a complete modernization of a task management application, rebuilt from the ground up
to serve as a portfolio project demonstrating best practices in modern .NET and web development.

**Target Users**: The primary user is a technical evaluator (e.g., a hiring manager or senior engineer)
assessing the developer's skills. The secondary user is an individual looking for a personal productivity tool.

**Core Value Proposition**: To showcase a clean, maintainable, performant, and well-tested application
built with a modern, end-to-end Microsoft-centric tech stack.

## 2. Core Features

### User & Identity Management

- User Registration
- User Login/Logout
- Secure Token-based API Authentication

### Task Management

- Create, Read, Update, and Delete (CRUD) tasks
- Assign tasks to Projects and Contexts
- Set due dates and completion status

### Organization

- CRUD operations for Projects
- CRUD operations for Contexts
- View tasks filtered by Project or Context

### Core Views

- **Inbox**: View all unorganized tasks.
- **Today**: View tasks due today.
- **Projects**: View tasks organized by project.
- **Contexts**: View tasks organized by context.
- **Completed**: View recently completed tasks.

## 3. Out-of-Scope Features

- **Multi-user collaboration**: The application is designed for a single user.
- **File attachments**: Tasks will not support file uploads in this version.
- **Advanced reporting**: No complex analytics or historical reporting will be built.

## 4. User Flows (Primary)

**Onboarding**: A new user registers for an account and logs in. They are presented with an empty inbox view.

**Core Task Workflow**: The user creates a new task in their Inbox. They then organize the task by assigning
it to a new or existing Project and Context. They can view the task in the "Today" view if the due date is
set for the current day. Once work is done, they mark the task as complete.

## 5. Success Metrics

- **Code Quality**: A clean, well-documented codebase with high marks from static analysis tools.
- **Test Coverage**: High unit and integration test coverage (target >80%) for the backend.
- **Functionality**: All core features are fully implemented and functional without bugs.
- **CI/CD**: A fully automated build, test, and deployment pipeline in GitHub Actions.
- **User Experience**: A polished, responsive, and intuitive web interface.

## 6. Technical Requirements

- **Performance**: API endpoints should respond in <200ms under normal load. The web application should
  achieve a high Lighthouse score for performance.
- **Reliability**: The application should be stable and handle common user errors gracefully.
- **Scalability**: The architecture should be based on patterns (Vertical Slices, stateless API) that allow
  for future scaling.
- **Security**: Adherence to standard web security practices (OWASP Top 10), including secure authentication,
  data protection, and proper input validation.

## 7. Stretch Goals

These features are not part of the core MVP but are high-value additions.
They should be considered after the primary features are complete.

- **Real-Time Client Sync**: Implement SignalR to provide real-time, push-based updates between a user's
  different clients (e.g., multiple browser tabs or a future mobile app), eliminating the need for manual
  refreshes.
- **Natural Language Task Input**: Create an API endpoint that integrates with an LLM (e.g., Gemini) to
  parse natural language input like "Call John tomorrow at 5pm" into a structured task with the correct
  title and due date.
- **Calendar View & Enhanced Dates**: Add a full calendar view to the UI for visualizing tasks over a month.
  This includes enhancing the `Task` domain model with `DeferUntilDate` and `CompletedDate` properties and
  creating the necessary API endpoints to support date-range queries.
