using AutoMapper;
using Caliburn.Micro;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TaskFocusDesktop.Utilities;
using TaskFocusDesktop.ViewModels;
using TaskFocusUI.Library;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Data.Services;
using TaskFocusUI.Library.Data.State;
using TaskFocusUI.Library.Logging;
using TaskFocusUI.Library.Models;
using TaskFocusUI.Library.Data.Services.Synchronization;
using TaskFocusUI.Library.Data.Services.Access;
using TaskFocusUI.Library.Data.Utilities;

namespace TaskFocusDesktop
{
    public class Bootstrapper : BootstrapperBase
    {
        private SimpleContainer _container = new SimpleContainer();
        public bool ShouldHandleEx { get; set; } = true;

        public Bootstrapper()
        {
            Initialize();

            ConventionManager.AddElementConvention<PasswordBox>(
            PasswordBoxHelper.BoundPasswordProperty,
            "Password",
            "PasswordChanged");
        }

        private IMapper ConfigureAutomapper()
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

        private IConfiguration AddConfiguration()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json");

            string configFileName = System.Diagnostics.Debugger.IsAttached ? 
                "appsettings.Development.json" : "appsettings.json";

            // inform user if appsettings missing
            string configFilePath = Path.Combine(AppContext.BaseDirectory, configFileName);
            if (!File.Exists(configFilePath))
            {
                System.Windows.MessageBox.Show("\"appsettings.json\" not found! " +
                    "Please ensure this config file (included with download) is in the same directory as the executable.",
                    "Taskfocus", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            builder.AddJsonFile(configFileName, optional: true, reloadOnChange: true);

            return builder.Build();
        }

        protected override void Configure()
        {
            // configure logging
            var loggerConfig = new CustomLoggerConfiguration()
            {
                ConsoleMinLogLevel = LogLevel.Information,
                InMemoryMinLogLevel = LogLevel.Warning
            };

            // custom caliburn-specific logging (ILog)
            LogManager.GetLog = type => new CaliburnLogger(type, loggerConfig);

            // dependency injection
            // ====================

            _container.Instance(ConfigureAutomapper());
            _container.RegisterInstance(typeof(IConfiguration), "IConfiguration", AddConfiguration());

            _container.Instance(_container)
                .PerRequest<IUserEndpoint, UserEndpoint>()
                .PerRequest<ITaskEndpoint, TaskEndpoint>()
                .PerRequest<IProjectEndpoint, ProjectEndpoint>()
                .PerRequest<IContextEndpoint, ContextEndpoint>()
                .PerRequest<IDataService, DataService>()
                .PerRequest<IDataSyncService, DataSyncService>();

            // use these singular instances
            _container
                .Singleton<IWindowManager, WindowManager>()
                .Singleton<IEventAggregator, EventAggregator>()
                .Singleton<ILoggedInUserModel, LoggedInUserModel>()
                .Singleton<IAPIHelper, APIHelper>()
                .Singleton<IDataHelper, DataHelper>()
                .Singleton<IDataState, DataState>()
                .Singleton<IAppState, AppState>();        

            // register view models - create new instance each time one is requested
            GetType().Assembly.GetTypes()
                .Where(type => type.IsClass)
                .Where(type => type.Name.EndsWith("ViewModel"))
                .ToList()
                .ForEach(viewModelType => _container.RegisterPerRequest(
                    viewModelType, viewModelType.ToString(), viewModelType));
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            DisplayRootViewForAsync<ShellViewModel>();
        }

        protected override object GetInstance(Type service, string key)
        {
            return _container.GetInstance(service, key);
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return _container.GetAllInstances(service);
        }

        protected override void BuildUp(object instance)
        {
            _container.BuildUp(instance);
        }

        protected void ShowException(Exception ex)
        {
            if (ex == null)
            {
                Execute.OnUIThread(() =>
                {
                    System.Windows.MessageBox.Show(ex.Message, "Taskfocus", MessageBoxButton.OK, MessageBoxImage.Information);
                    Console.WriteLine($"{ex.Source} threw an exception:", ex.Message);
                });
            }
        }

        protected override void OnUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            ShowException(e.Exception);
            e.Handled = ShouldHandleEx ? true : false;
        }
    }
}
