# TaskFocus

Personal task management application with both native desktop and web-based user interfaces.  
Allows users to create and manage their to-do items, using concepts from the Getting Things Done (GTD) productivity system.  
Full-stack application built with C# and .NET, with continuous integration and deployment via Azure DevOps pipelines.  

 Today | Projects | Contexts | Settings
|------------|-------------|-------------|-------------|
| <img src="https://github.com/user-attachments/assets/c2da97b3-39d6-47af-a9ee-be2e7a75714c" width="250"> | <img src="https://github.com/user-attachments/assets/fccab5df-9227-48d8-8721-8ebad11147ce" width="250"> | <img src="https://github.com/user-attachments/assets/cd4be72c-c171-40fc-9758-3d73b11ae287" width="250"> | <img src="https://github.com/user-attachments/assets/0aa0ec16-8414-4333-9d26-95c3c60f76ab" width="250"> |  

The app utilizes both dependency injection and class libraries for modularity/separation of concerns.  
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

## Usage
For demo usage, please register as a new user.
Please note that email confirmation is required.  
Desktop app (Windows):  [Download](RELEASE URL - UPDATE)
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
-- merge conflict logic
