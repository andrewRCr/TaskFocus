using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Models;
using TaskFocusWeb.Authentication;

namespace TaskFocusWeb
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

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

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddMudServices();

            await builder.Build().RunAsync();
        }
    }
}
