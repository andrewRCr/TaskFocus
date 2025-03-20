using AutoMapper;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudExtensions.Services;
using MudBlazor.Services;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Logging;
using TaskFocusUI.Library.Models;
using TaskFocusWeb.Authentication;
using TaskFocusUI.Library.Data.State;
using TaskFocusUI.Library.Data.Services;
using TaskFocusUI.Library.Data.Services.Synchronization;
using TaskFocusUI.Library.Data.Services.Access;
using TaskFocusUI.Library.Data.Utilities;

namespace TaskFocusWeb
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            // configure logging
            var loggerConfig = new CustomLoggerConfiguration()
            {
                ConsoleMinLogLevel = LogLevel.Information,
                InMemoryMinLogLevel = LogLevel.Warning
            };
            var memoryLog = new InMemoryLog();
            builder.Services.AddSingleton(memoryLog);
            builder.Logging.AddProvider(new CustomLoggerProvider(loggerConfig, memoryLog));

            // dependency injection
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
            builder.Services.AddBlazoredLocalStorage();
            builder.Services.AddAuthorizationCore();
            builder.Services.AddScoped<AuthenticationStateProvider, AuthStateProvider>();
            builder.Services.AddSingleton<IAPIHelper, APIHelper>();
            builder.Services.AddSingleton<ILoggedInUserModel, LoggedInUserModel>();
            builder.Services.AddTransient<IUserEndpoint, UserEndpoint>();
            builder.Services.AddTransient<ITaskEndpoint, TaskEndpoint>();
            builder.Services.AddTransient<IProjectEndpoint, ProjectEndpoint>();
            builder.Services.AddTransient<IContextEndpoint, ContextEndpoint>();
            builder.Services.AddSingleton(new AppState());
            builder.Services.AddSingleton<IDataState, DataState>();
            builder.Services.AddSingleton<IDataHelper, DataHelper>();
            builder.Services.AddScoped<IDataService, DataService>();
            builder.Services.AddScoped<IDataSyncService, DataSyncService>();

            IMapper ConfigureAutomapper()
            {
                var config = new MapperConfiguration(cfg =>
                {
                    cfg.CreateMap<TaskModel, TaskDisplayModel>();
                    cfg.CreateMap<TaskDisplayModel, TaskModel>();
                    cfg.CreateMap<ProjectModel, ProjectDisplayModel>();
                    cfg.CreateMap<ProjectDisplayModel, ProjectModel>();
                    cfg.CreateMap<ContextModel, ContextDisplayModel>();
                    cfg.CreateMap<ContextDisplayModel, ContextModel>();
                    cfg.CreateMap<UserSettingsModel, UserSettingsDisplayModel>();
                    cfg.CreateMap<UserSettingsDisplayModel, UserSettingsModel>();
                    cfg.CreateMap<UserModel, UserDisplayModel>();
                    cfg.CreateMap<UserDisplayModel, UserModel>();
                });

                return config.CreateMapper();
            }

            builder.Services.AddSingleton(ConfigureAutomapper());

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddMudServices(config =>
            {
                config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;

                config.SnackbarConfiguration.PreventDuplicates = false;
                config.SnackbarConfiguration.NewestOnTop = false;
                config.SnackbarConfiguration.ShowCloseIcon = true;
                config.SnackbarConfiguration.VisibleStateDuration = 7500;
                config.SnackbarConfiguration.HideTransitionDuration = 500;
                config.SnackbarConfiguration.ShowTransitionDuration = 500;
                config.SnackbarConfiguration.SnackbarVariant = Variant.Outlined;
                config.SnackbarConfiguration.ClearAfterNavigation = false;
            });
            builder.Services.AddMudExtensions();

            await builder.Build().RunAsync();
        }
    }
}
