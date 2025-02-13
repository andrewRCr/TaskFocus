using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using TaskFocusAPI.Data;
using TaskFocusAPI.Library.DataAccess;
using TaskFocusAPI.Library.Utilities;

namespace TaskFocusAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // add services to the container
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            builder.Services.AddControllersWithViews();

            builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromHours(10);
            });

            builder.Services.Configure<IdentityOptions>(options => options.SignIn.RequireConfirmedEmail = true);

            builder.Services.AddCors(policy =>
            {
                policy.AddPolicy("OpenCorsPolicy", opt =>
                    opt.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                );
            });

            // internal services
            builder.Services.AddTransient<IEmailSender, EmailSender>();
            builder.Services.AddTransient<IUserData, UserData>();
            builder.Services.AddTransient<ITaskData, TaskData>();
            builder.Services.AddTransient<IProjectData, ProjectData>();
            builder.Services.AddTransient<IContextData, ContextData>();
            builder.Services.AddTransient<ISqlDataAccess, SqlDataAccess>();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "JwtBearer";
                options.DefaultChallengeScheme = "JwtBearer";
            })
                .AddJwtBearer("JwtBearer", jwtBearerOptions =>
                {
                    // pull security key from key vault
                    string? securityKey = null;
                    string keyVaultUrl = builder.Configuration.GetValue<string>("AzureKeyVaultUrl")!;
                    var secretsClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
                    securityKey = secretsClient.GetSecret("JwtSecurityKey").Value.Value;

                    if (securityKey != null)
                    {
                        jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey)),
                            ValidateIssuer = false,
                            ValidateAudience = false,
                            ValidateLifetime = true,
                            ClockSkew = TimeSpan.FromMinutes(5)
                        };
                    }
                    else
                    {
                        throw new Exception("SecurityKey was a null value!");
                    }
                });

            builder.Services.AddSwaggerGen(setup =>
            {
                setup.SwaggerDoc(
                    "v1",
                    new OpenApiInfo
                    {
                        Title = "TaskFocus API",
                        Version = "v1",
                    });
            });

            var app = builder.Build();

            // configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // the default HSTS value is 30 days; see https://aka.ms/aspnetcore-hsts
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseCors("OpenCorsPolicy");
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSwagger();
            app.UseSwaggerUI(x =>
            {
                x.RoutePrefix = ""; // launch directly into swagger
                x.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskFocus API v1");
            });

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            app.Run();
        }
    }
}
