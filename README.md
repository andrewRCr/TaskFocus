# TaskFocus

> TaskFocus v1.0 is live. A v2.0 modernization is in progress — see
> [docs/MODERNIZATION.md](docs/MODERNIZATION.md) for details.

A cross-platform task management application built on the Getting Things Done productivity
methodology, with both a web interface and a native Windows desktop client powered by a
shared .NET 8 backend. The GTD domain model organizes tasks along two independent axes —
projects group related work, contexts group tasks by the conditions needed to complete
them — with each view maintaining its own drag-and-drop sort order. Both clients stay
synchronized through bi-directional background polling and manual sync triggers,
reconciling task state across platforms.

<p align="center">
  <a href="https://taskfocus.andrewcreekmore.dev">Live Demo</a>
  &nbsp;&nbsp;|&nbsp;&nbsp;
  <a href="https://github.com/andrewRCr/TaskFocus/releases/latest">Download (Windows)</a>
  &nbsp;&nbsp;|&nbsp;&nbsp;
  <a href="https://andrewcreekmore.dev/projects/software/taskfocus">Portfolio</a>
</p>

<div align="center">
  <a href="https://github.com/user-attachments/assets/c2da97b3-39d6-47af-a9ee-be2e7a75714c"><img src="https://github.com/user-attachments/assets/c2da97b3-39d6-47af-a9ee-be2e7a75714c" width="24%" alt="Today view" /></a>
  <a href="https://github.com/user-attachments/assets/fccab5df-9227-48d8-8721-8ebad11147ce"><img src="https://github.com/user-attachments/assets/fccab5df-9227-48d8-8721-8ebad11147ce" width="24%" alt="Projects view" /></a>
  <a href="https://github.com/user-attachments/assets/cd4be72c-c171-40fc-9758-3d73b11ae287"><img src="https://github.com/user-attachments/assets/cd4be72c-c171-40fc-9758-3d73b11ae287" width="24%" alt="Contexts view" /></a>
  <a href="https://github.com/user-attachments/assets/0aa0ec16-8414-4333-9d26-95c3c60f76ab"><img src="https://github.com/user-attachments/assets/0aa0ec16-8414-4333-9d26-95c3c60f76ab" width="24%" alt="Settings" /></a>
</div>

## Details

*Users organize tasks following GTD methodology across web and desktop interfaces, with
task assignments, ordering, and completions syncing automatically between platforms.*

- GTD-inspired views — Inbox, Today, Projects, Contexts, and Completed — with per-view
  drag-and-drop ordering
- Bi-directional data synchronization between web and desktop clients via background
  polling and manual trigger
- Task lifecycle management with user-configurable intervals for automatic clean-up
  and eventual deletion
- JWT authentication with transactional email for address confirmation, password reset,
  and account management
- Blazor Web Assembly frontend with MudBlazor component library for a responsive
  Material Design interface
- WPF desktop client following MVVM with Caliburn Micro for convention-based view binding
  and screen lifecycle management
- View-relative ordering: every view maintains its own independent sort indices, synced
  alongside but separately from task data

## Technology

- **Frontend (Web):** Blazor Web Assembly, MudBlazor
- **Frontend (Desktop):** WPF, Caliburn Micro
- **Backend:** C#, .NET 8, ASP.NET Identity, JWT
- **Database:** SQL Server, Entity Framework
- **Infrastructure:** Azure DevOps CI/CD

<p align="center"><a href="#taskfocus">↑ Back to top</a></p>
