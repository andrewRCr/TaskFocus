using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace TaskFocusDesktop.Utilities
{
    public static class ConfigExtension
    {
        public static void AddEmbeddedJsonFile(this IConfigurationBuilder configuration, string resourceName)
        {
            var host = Assembly.GetEntryAssembly();
            if (host == null)
                return;

            var fullFileName = $"{host.GetName().Name}.{resourceName}";
            using var input = host.GetManifestResourceStream(fullFileName);
            if (input != null)
            {
                configuration.AddJsonStream(input);
            }
        }
    }
}
