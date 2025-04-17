# TaskFocus

Personal task management application with both native desktop and web-based user interfaces. Allows users to create and manage to-do items, using concepts from the Getting Things Done (GTD) productivity system. Full-stack application built with C# and .NET, with continuous integration and deployment via Azure DevOps pipelines. Utilizes both dependency injection and class libraries for separation of concerns.  

 Today view | Projects view | Contexts view | Settings view
|------------|-------------|-------------|-------------|
| <img src="https://github.com/user-attachments/assets/c88bc20c-60cf-4b83-9ffa-152124f01990" width="250"> | <img src="https://github.com/user-attachments/assets/4f1f6bff-7154-4d1e-8709-bc734c6c80cb" width="250"> | <img src="https://github.com/user-attachments/assets/5ded333b-56f3-4f5a-b001-0179577c0b6e" width="250"> | <img src="https://github.com/user-attachments/assets/87c99d45-f895-45fe-90d7-69b7cd96d0d7" width="250"> |  

Web app built using Blazor Web Assembly (WASM) and MudBlazor.  
Desktop app built using WPF and Caliburn Micro following the Model-View-ViewModel (MVVM) architectural pattern.  
Backend consists of an API built with .NET 8 and SQL Server databases, using Identity, JSON Web Tokens (JWT), and Entity Framework for mapping of user account data.  

## Overview  
Users can create and organize task data, which is synced between the web and desktop apps.
Following the GTD productivity system, tasks can be assigned both projects and contexts:
- *projects* group tasks by relation or dependency
- *contexts* group tasks that will be performed in similar conditions

An Inbox view serves as a default bucket for new tasks that haven't yet had projects/contexts assigned.
Once fully assigned, tasks are moved from the Inbox view to both their relevant Project and Context views.
In these views, tasks can be ordered (via drag/drop) arbitrarily and independently (view-relative indexing).

Tasks can also be given due dates and/or "starred", and such tasks will automatically populate the Today view. 
The Completed view displays tasks marked done but not yet deleted. 
Completed tasks can also remain in their original view for a user-defined length of time before being "cleaned up" - i.e., moved to the Completed view exclusively prior to eventual deletion.
The parameters for these automatic clean-up and deletion intervals are exposed as user settings.

Features user authentication and authorization, including transactional email with automated email address confirmation and email address/username and password change/reset functionality.  

Bi-directional data synchronization is performed automatically in the background on a periodic interval, but can also be triggered manually by the user.

## Usage
For demo usage, please register as a new user. Note that email confirmation is required.  

Desktop app (Windows): [Download](https://github.com/andrewRCr/TaskFocus/releases/latest)  
Web app: [taskfocus.andrewcreekmore.com](https://taskfocus.andrewcreekmore.com)

## Future Development
- overall UX improvements:
-- desktop app: minimal "docked mode" version of UI and global inbox quick-entry widget
-- improved mobile web app UX and/or dedicated, native mobile app
- app domain content feature improvements:
-- nested sub-collections (projects, contexts) and sub-tasks
-- support for repeating tasks and defer/start dates
-- CalDAV support for external calendar integration
- synchronization improvements:
-- offline support - persistent local data storage for both clients, with merge handling when reconnected
-- column-level differential merge conflict logic
