using TaskFocusUI.Library.Models;

namespace TaskFocusWeb
{
    public interface IDataService
    {
        Task AddContext(ContextModel newContext);
        Task AddProject(ProjectModel newProject);
        Task AssignContextIdFromContextName(TaskModel task);
        Task AssignProjectIdFromProjectName(TaskModel task);
        Task FetchAllRemoteData();
        Task FetchRemoteContextData();
        Task FetchRemoteProjectData();
        Task FetchRemoteTaskData();
        Task UpdateContextData(ContextDisplayModel displayContext);
        Task UpdateProjectData(ProjectDisplayModel displayProject);
        Task UpdateTaskData(TaskDisplayModel displayTask);
    }
}