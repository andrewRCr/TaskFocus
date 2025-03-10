using System.Collections.Generic;
using System.Threading.Tasks;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Utilities
{
    public interface IDataService
    {
        Task FetchAllRemoteData();
        Task FetchRemoteUserData();
        Task FetchRemoteTaskData();
        Task FetchRemoteProjectData();
        Task FetchRemoteContextData();
        Task FetchRemoteProjectAndTasksById(int id);
        Task FetchRemoteContextAndTasksById(int id);

        Task AddTask(TaskDisplayModel displayTask);
        Task DeleteTask(TaskDisplayModel displayTask);
        Task UpdateTaskData(TaskDisplayModel displayTask, bool forceUpdate = false);
        void UpdateTaskViewOrderingIndices(List<TaskDisplayModel> displayTasks);
        void ShiftTaskCollectionSourceIndices(TaskModel task, string indexType);
        void HandleIndexShiftsOnTaskDeletion(TaskModel task);
        Task HandleTaskProjectChanged(TaskModel task);
        Task HandleTaskContextChanged(TaskModel task);

        Task AddProject(ProjectModel newProject);
        Task DeleteProject(ProjectDisplayModel displayProject);
        Task UpdateProjectData(ProjectDisplayModel displayProject);
        void UpdateProjectsOrderingIndices(List<ProjectDisplayModel> displayProjects);

        Task AddContext(ContextModel newContext);
        Task DeleteContext(ContextDisplayModel displayContext);
        Task UpdateContextData(ContextDisplayModel displayContext);
        Task UpdateContextsOrderingIndices(List<ContextDisplayModel> displayContexts);

        void ShiftCollectionOrderIndices<T>(T collectionDisplayModel, List<T> collectionSource) where T : ICollectionDisplayModel;

        Task<bool> CheckUserExists(UserModel user);
        Task UpdateUserNameData(UserDisplayModel displayUserModel);
        Task RequestUpdateEmail(UserModel user);
        Task UpdatePassword(CreateUserModel updatedUserModel);
        Task UpdateSettingsData(UserSettingsDisplayModel displaySettings);
    }
}