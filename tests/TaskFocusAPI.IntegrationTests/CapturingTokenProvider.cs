using System.Collections.Concurrent;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TaskFocusAPI.IntegrationTests
{
    internal sealed class CapturingTokenProvider(
        IDataProtectionProvider dataProtectionProvider,
        IOptions<DataProtectionTokenProviderOptions> options,
        ILogger<DataProtectorTokenProvider<IdentityUser>> logger)
        : DataProtectorTokenProvider<IdentityUser>(dataProtectionProvider, options, logger)
    {
        private const string Prefix = "+/=";

        internal ConcurrentDictionary<string, string> GeneratedTokens { get; } = new();

        public override async Task<string> GenerateAsync(string purpose, UserManager<IdentityUser> manager, IdentityUser user)
        {
            // Guarantee reserved characters while retaining normal Identity validation of the protected token.
            string token = Prefix + await base.GenerateAsync(purpose, manager, user);
            GeneratedTokens[user.Email!] = token;
            return token;
        }

        public override Task<bool> ValidateAsync(string purpose, string token, UserManager<IdentityUser> manager, IdentityUser user)
        {
            return token.StartsWith(Prefix, StringComparison.Ordinal)
                ? base.ValidateAsync(purpose, token[Prefix.Length..], manager, user)
                : Task.FromResult(false);
        }
    }
}
