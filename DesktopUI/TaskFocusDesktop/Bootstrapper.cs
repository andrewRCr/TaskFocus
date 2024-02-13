using AutoMapper;
using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TaskFocusDesktop.Library.API;
using TaskFocusDesktop.Library.Models;
using TaskFocusDesktop.Models;
using TaskFocusDesktop.Utilities;
using TaskFocusDesktop.ViewModels;

namespace TaskFocusDesktop
{
    public class Bootstrapper : BootstrapperBase
    {
        private SimpleContainer _container = new SimpleContainer();

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
            });

            return config.CreateMapper();
        }

        protected override void Configure()
        {

            // dependency injection
            // ====================

            _container.Instance(ConfigureAutomapper());

            _container.Instance(_container)
                .PerRequest<IUserEndpoint, UserEndpoint>()
                .PerRequest<ITaskEndpoint, TaskEndpoint>()
                .PerRequest<IProjectEndpoint, ProjectEndpoint>();

            // use these singular instances
            _container
                .Singleton<IWindowManager, WindowManager>()
                .Singleton<IEventAggregator, EventAggregator>()
                .Singleton<ILoggedInUserModel, LoggedInUserModel>()
                .Singleton<IAPIHelper, APIHelper>();

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
    }
}
