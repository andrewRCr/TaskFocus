using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace TaskFocusAPI.Services
{
    public interface IJwtKeyProvider
    {
        SymmetricSecurityKey SigningKey { get; }
    }

    public sealed class JwtKeyProvider(
        IConfiguration configuration,
        ILogger<JwtKeyProvider> logger,
        TimeProvider? timeProvider = null,
        Func<string>? readVaultSecret = null) : IJwtKeyProvider
    {
        private static readonly TimeSpan RetryCooldown = TimeSpan.FromSeconds(30);
        private readonly object _lock = new();
        private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
        private SymmetricSecurityKey? _signingKey;
        private long? _lastFailureTimestamp;

        public SymmetricSecurityKey SigningKey
        {
            get
            {
                lock (_lock)
                {
                    if (_signingKey is not null)
                    {
                        return _signingKey;
                    }

                    if (_lastFailureTimestamp is long failedAt && _timeProvider.GetElapsedTime(failedAt) < RetryCooldown)
                    {
                        throw new InvalidOperationException("The JWT signing key is temporarily unavailable.");
                    }

                    try
                    {
                        return _signingKey = ReadSigningKey();
                    }
                    catch (Exception ex)
                    {
                        _lastFailureTimestamp = _timeProvider.GetTimestamp();
                        logger.LogError(ex, "Failed to load the JWT signing key.");
                        throw;
                    }
                }
            }
        }

        private SymmetricSecurityKey ReadSigningKey()
        {
            string securityKey = configuration["JwtSecurityKey"] ?? (readVaultSecret ?? ReadVaultSecret)();
            byte[] keyBytes = Encoding.UTF8.GetBytes(securityKey);
            if (keyBytes.Length < 32)
            {
                throw new InvalidOperationException("JwtSecurityKey must be at least 32 UTF-8 bytes for HS256.");
            }

            return new SymmetricSecurityKey(keyBytes);
        }

        private string ReadVaultSecret()
        {
            string keyVaultUrl = configuration["AzureKeyVaultUrl"]!;
            var secretsClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
            return secretsClient.GetSecret("JwtSecurityKey").Value.Value;
        }
    }
}
