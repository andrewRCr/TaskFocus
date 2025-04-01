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
                                    List<TaskDisplayModel> tasks) => ((IDataStateInternal)iface).Tasks = tasks;

        public static List<TaskDisplayModel>? GetWorkingTasks(this IDataState iface) => ((IDataStateInternal)iface).WorkingTasks;

        public static void SetWorkingTasks(this IDataState iface,
                                           List<TaskDisplayModel> tasks) => ((IDataStateInternal)iface).WorkingTasks = tasks;

        public static List<ProjectDisplayModel>? GetProjects(this IDataState iface) => ((IDataStateInternal)iface).Projects;

        public static void SetProjects(this IDataState iface,
                                       List<ProjectDisplayModel> projects) => ((IDataStateInternal)iface).Projects = projects;

        public static List<ProjectDisplayModel>? GetWorkingProjects(this IDataState iface) => ((IDataStateInternal)iface).WorkingProjects;

        public static void SetWorkingProjects(this IDataState iface,
                                              List<ProjectDisplayModel> projects) => ((IDataStateInternal)iface).WorkingProjects = projects;

        public static List<ContextDisplayModel>? GetContexts(this IDataState iface) => ((IDataStateInternal)iface).Contexts;

        public static void SetContexts(this IDataState iface,
                                              List<ContextDisplayModel> contexts) => ((IDataStateInternal)iface).Contexts = contexts;

        public static List<ContextDisplayModel>? GetWorkingContexts(this IDataState iface) => ((IDataStateInternal)iface).WorkingContexts;

        public static void SetWorkingContexts(this IDataState iface,
                                              List<ContextDisplayModel> contexts) => ((IDataStateInternal)iface).WorkingContexts = contexts;
    }
}
