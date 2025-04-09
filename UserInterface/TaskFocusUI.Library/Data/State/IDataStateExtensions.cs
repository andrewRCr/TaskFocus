using System;
using System.Collections.Generic;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Data.State
{
    internal static class IDataStateExtensions
    {
        public static UserDisplayModel? GetCurrentUser(this IDataState iface) => ((IDataStateInternal)iface).CurrentUser;

        public static void SetCurrentUser(this IDataState iface, 
                                          UserDisplayModel? user) => ((IDataStateInternal)iface).CurrentUser = user;

        public static UserDisplayModel? GetWorkingCurrentUser(this IDataState iface) => ((IDataStateInternal)iface).WorkingCurrentUser;

        public static void SetWorkingCurrentUser(this IDataState iface,
                                          UserDisplayModel? workingUser) => ((IDataStateInternal)iface).WorkingCurrentUser = workingUser;

        public static UserSettingsDisplayModel? GetUserSettings(this IDataState iface) => ((IDataStateInternal)iface).UserSettings;

        public static void SetUserSettings(this IDataState iface,
                                           UserSettingsDisplayModel? userSettings) => ((IDataStateInternal)iface).UserSettings = userSettings;

        public static UserSettingsDisplayModel? GetWorkingUserSettings(this IDataState iface) => ((IDataStateInternal)iface).WorkingUserSettings;

        public static void SetWorkingUserSettings(this IDataState iface,
                                           UserSettingsDisplayModel? workingUserSettings) => ((IDataStateInternal)iface).WorkingUserSettings = workingUserSettings;

        public static List<TaskDisplayModel>? GetTasks(this IDataState iface) => ((IDataStateInternal)iface).Tasks;

        public static void SetTasks(this IDataState iface,
                                    List<TaskDisplayModel>? tasks) => ((IDataStateInternal)iface).Tasks = tasks;

        public static List<TaskDisplayModel>? GetWorkingTasks(this IDataState iface) => ((IDataStateInternal)iface).WorkingTasks;

        public static void SetWorkingTasks(this IDataState iface,
                                           List<TaskDisplayModel>? tasks) => ((IDataStateInternal)iface).WorkingTasks = tasks;

        public static List<ProjectDisplayModel>? GetProjects(this IDataState iface) => ((IDataStateInternal)iface).Projects;

        public static void SetProjects(this IDataState iface,
                                       List<ProjectDisplayModel>? projects) => ((IDataStateInternal)iface).Projects = projects;

        public static List<ProjectDisplayModel>? GetWorkingProjects(this IDataState iface) => ((IDataStateInternal)iface).WorkingProjects;

        public static void SetWorkingProjects(this IDataState iface,
                                              List<ProjectDisplayModel>? projects) => ((IDataStateInternal)iface).WorkingProjects = projects;

        public static List<ContextDisplayModel>? GetContexts(this IDataState iface) => ((IDataStateInternal)iface).Contexts;

        public static void SetContexts(this IDataState iface,
                                              List<ContextDisplayModel>? contexts) => ((IDataStateInternal)iface).Contexts = contexts;

        public static List<ContextDisplayModel>? GetWorkingContexts(this IDataState iface) => ((IDataStateInternal)iface).WorkingContexts;

        public static void SetWorkingContexts(this IDataState iface,
                                              List<ContextDisplayModel>? contexts) => ((IDataStateInternal)iface).WorkingContexts = contexts;

        public static DateTimeOffset GetLastSync(this IDataState iface) => ((IDataStateInternal)iface).LastSync;

        public static void SetLastSync(this IDataState iface,
                                          DateTimeOffset time) => ((IDataStateInternal)iface).LastSync = time;

        //public static bool GetPreLogoutSyncCompleted(this IDataState iface) => ((IDataStateInternal)iface).PreLogoutSyncCompleted;

        //public static void SetPreLogoutSyncCompleted(this IDataState iface,
        //                                  bool completed) => ((IDataStateInternal)iface).PreLogoutSyncCompleted = completed;

        public static bool GetAppRequestedSyncCompleted(this IDataState iface) => ((IDataStateInternal)iface).AppRequestedSyncCompleted;

        public static void SetAppRequestedSyncCompleted(this IDataState iface,
                                          bool completed) => ((IDataStateInternal)iface).AppRequestedSyncCompleted = completed;

        public static UserDisplayModel? GetChangedUserData(this IDataState iface) => ((IDataStateInternal)iface).ChangedUserData;

        public static void SetChangedUserData(this IDataState iface,
                                          UserDisplayModel? changedUser) => ((IDataStateInternal)iface).ChangedUserData = changedUser;

        public static UserSettingsDisplayModel? GetChangedSettingsData(this IDataState iface) => ((IDataStateInternal)iface).ChangedUserSettingsData;

        public static void SetChangedSettingsData(this IDataState iface,
                                          UserSettingsDisplayModel? changedSettings) => ((IDataStateInternal)iface).ChangedUserSettingsData = changedSettings;

        public static List<TaskDisplayModel> GetChangedTaskData(this IDataState iface) => ((IDataStateInternal)iface).ChangedTaskData;

        public static void SetChangedTaskData(this IDataState iface,
                                          List<TaskDisplayModel> changedTasks) => ((IDataStateInternal)iface).ChangedTaskData = changedTasks;

        public static List<ProjectDisplayModel> GetChangedProjectData(this IDataState iface) => ((IDataStateInternal)iface).ChangedProjectData;

        public static void SetChangedProjectData(this IDataState iface,
                                          List<ProjectDisplayModel> changedProjects) => ((IDataStateInternal)iface).ChangedProjectData = changedProjects;

        public static List<ContextDisplayModel> GetChangedContextData(this IDataState iface) => ((IDataStateInternal)iface).ChangedContextData;

        public static void SetChangedContextData(this IDataState iface,
                                          List<ContextDisplayModel> changedContexts) => ((IDataStateInternal)iface).ChangedContextData = changedContexts;

        public static int GetTempTaskId(this IDataState iface) => ((IDataStateInternal)iface).TempTaskId;

        public static int SetTempTaskId(this IDataState iface,
                                          int newValue) => ((IDataStateInternal)iface).TempTaskId = newValue;

        public static int GetTempProjectId(this IDataState iface) => ((IDataStateInternal)iface).TempProjectId;

        public static int SetTempProjectId(this IDataState iface,
                                          int newValue) => ((IDataStateInternal)iface).TempProjectId = newValue;

        public static int GetTempContextId(this IDataState iface) => ((IDataStateInternal)iface).TempContextId;

        public static int SetTempContextId(this IDataState iface,
                                          int newValue) => ((IDataStateInternal)iface).TempContextId = newValue;

        public static bool IsDataLoaded(this IDataState iface) => ((IDataStateInternal)iface).IsDataLoaded();       
    }
}
