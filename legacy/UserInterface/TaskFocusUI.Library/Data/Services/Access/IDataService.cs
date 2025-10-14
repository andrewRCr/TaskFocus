using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskFocusUI.Library.Data.State;
using TaskFocusUI.Library.Data.Utilities;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Data.Services.Access
{
    public interface IDataService
    {
        event EventHandler<string>? SyncRequestHandler;
        event EventHandler<string>? SyncWithCompletionNotifyRequestHandler;
        void InvokeSyncRequest(string sourceName, bool notifyOnCompletion = false);

        bool IsDataStateLoaded();
        bool IsAppRequestedSyncCompleted();
        DateTimeOffset GetDataStateLastSync();
        DataSyncResult GetDataStateLastSyncResult();
        ESyncStatus GetCurrentSyncStatus();
        int GetSyncIntervalSeconds();
        void ResetDataStateOnLogout();

        Task FetchAllRemoteData();
        Task FetchRemoteUserData();
        Task FetchRemoteTaskData();
        Task FetchRemoteProjectData();
        Task FetchRemoteContextData();
        Task FetchRemoteProjectAndTasksById(int id);
        Task FetchRemoteContextAndTasksById(int id);

        UserDisplayModel? GetDataStateCurrentUser();
        Task<UserModel> GetRawCurrentUserData();
        UserSettingsDisplayModel? GetDataStateUserSettings();
        List<TaskDisplayModel>? GetDataStateTasks();
        List<ProjectDisplayModel>? GetDataStateProjects();
        List<ContextDisplayModel>? GetDataStateContexts();

        void AddTask(TaskDisplayModel displayTask);
        void DeleteTask(TaskDisplayModel displayTask);
        void UpdateTaskData(TaskDisplayModel displayTask, bool forceUpdate = false);
        bool IsTaskCurrentlyBeingUpdated(TaskDisplayModel task);
        List<TaskDisplayModel> GetTodayTasks();

        ProjectDisplayModel? AddProject(ProjectModel newProject);
        void DeleteProject(ProjectDisplayModel displayProject);
        void UpdateProjectData(ProjectDisplayModel displayProject);
        bool IsProjectCurrentlyBeingUpdated(ProjectDisplayModel project);

        ContextDisplayModel? AddContext(ContextModel newContext);
        void DeleteContext(ContextDisplayModel displayContext);
        void UpdateContextData(ContextDisplayModel displayContext);
        bool IsContextCurrentlyBeingUpdated(ContextDisplayModel context);

        Task<bool> CheckUserExists(UserModel user);
        Task<bool> CheckUserEmailConfirmed(UserModel userModel);
        Task<bool> CheckPasswordValid(CheckPasswordModel checkPasswordModel);

        Task CreateUser(CreateUserModel userModel);
        void UpdateUserNameData(UserDisplayModel displayUserModel);
        Task<bool> RequestUpdateEmail(UserModel user);
        Task ConfirmEmail(ConfirmEmailModel confirmEmailModel);
        Task ConfirmUpdatedEmail(ConfirmUpdatedEmailModel confirmUpdatedEmailModel);

        Task UpdatePassword(CreateUserModel updatedUserModel);
        Task ResetPassword(ResetPasswordModel resetPasswordModel);

        void UpdateSettingsData(UserSettingsDisplayModel workingSettings);

        Task SendEmailConfirmationLink(UserModel userModel);
        Task SendPasswordResetEmail(UserModel userModel);
        Task SendPasswordChangeSuccessEmail(UserModel user);
    }
}