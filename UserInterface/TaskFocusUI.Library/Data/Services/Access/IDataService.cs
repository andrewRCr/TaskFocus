using System.Collections.Generic;
using System.Threading.Tasks;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Data.Services.Access
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

        List<TaskDisplayModel>? GetDataStateTasks();
        List<ProjectDisplayModel>? GetDataStateProjects();
        List<ContextDisplayModel>? GetDataStateContexts();

        Task AddTask(TaskDisplayModel displayTask);
        void DeleteTask(TaskDisplayModel displayTask);
        Task UpdateTaskData(TaskDisplayModel displayTask, bool forceUpdate = false);
        bool IsTaskCurrentlyBeingUpdated(TaskDisplayModel task);

        ProjectDisplayModel? AddProject(ProjectModel newProject);
        Task DeleteProject(ProjectDisplayModel displayProject);
        Task UpdateProjectData(ProjectDisplayModel displayProject);

        Task AddContext(ContextModel newContext);
        Task DeleteContext(ContextDisplayModel displayContext);
        Task UpdateContextData(ContextDisplayModel displayContext);

        Task<bool> CheckUserExists(UserModel user);
        Task UpdateUserNameData(UserDisplayModel displayUserModel);
        Task RequestUpdateEmail(UserModel user);
        Task UpdatePassword(CreateUserModel updatedUserModel);
        Task UpdateSettingsData(UserSettingsDisplayModel displaySettings);

        // made private:
        //void ShiftTaskCollectionSourceIndices(TaskModel task, string indexType);
        //Task HandleTaskProjectChanged(TaskModel task);
        //Task HandleTaskContextChanged(TaskDisplayModel task);

        // made internal:
        //void HandleIndexShiftsOnTaskDeletion(TaskDisplayModel task);
        //void ShiftCollectionOrderIndices<T>(T collectionDisplayModel, List<T> collectionSource) where T : ICollectionDisplayModel;

        // removed:
        //Task UpdateTaskViewOrderingIndices(List<TaskDisplayModel> displayTasks);
        //void UpdateProjectsOrderingIndices(List<ProjectDisplayModel> displayProjects);
        //Task UpdateContextsOrderingIndices(List<ContextDisplayModel> displayContexts);
    }
}