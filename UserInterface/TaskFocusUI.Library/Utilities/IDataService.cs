using System.Collections.Generic;
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
        Task FetchRemoteUserData();
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
        Task UpdateCollectionOrderingIndices(List<TaskDisplayModel> displayTasks);
        Task FetchRemoteProjectAndTasksById(int id);
        Task FetchRemoteContextAndTasksById(int id);
        Task UpdateProjectsOrderingIndices(List<ProjectDisplayModel> displayProjects);
        Task UpdateContextsOrderingIndices(List<ContextDisplayModel> displayContexts);
        Task UpdateUserNameData(UserDisplayModel displayUserModel);
        Task RequestUpdateEmail(UserModel user);
        Task<bool> CheckUserExists(UserModel user);
        Task UpdatePassword(CreateUserModel updatedUserModel);
        void HandleIndexShiftsOnTaskDeletion(TaskModel task);
        //Task SyncClientServerData();
    }
}