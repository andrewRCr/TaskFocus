using System.Threading.Tasks;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Utilities
{
    public interface IDataService
    {
        Task AddContext(ContextModel newContext);
        Task AddProject(ProjectModel newProject);
        Task HandleTaskContextChanged(TaskModel task);
        Task HandleTaskProjectChanged(TaskModel task);
        Task FetchAllRemoteData();
        Task FetchRemoteTaskData();
        Task FetchRemoteProjectData();
        Task FetchRemoteContextData();
        Task UpdateContextData(ContextDisplayModel displayContext);
        Task UpdateProjectData(ProjectDisplayModel displayProject);
        Task UpdateTaskData(TaskDisplayModel displayTask, bool forceUpdate = false);
        Task DeleteTask(TaskDisplayModel displayTask);
        Task AddTask(TaskDisplayModel displayTask);
        Task DeleteProject(ProjectDisplayModel displayProject);
        Task DeleteContext(ContextDisplayModel displayContext);
        Task UpdateSettingsData(UserSettingsDisplayModel displaySettings);
        TaskModel MapToRawTask(TaskDisplayModel displayTask);
        void ShiftTaskCollectionSourceIndices(TaskModel task, string indexType);
    }
}