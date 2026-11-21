using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using MimeKit;
using TaskFocusAPI.Library.Utilities;

namespace TaskFocusAPI.IntegrationTests
{
    internal sealed class CapturingEmailSender(IConfiguration configuration) : EmailSender(configuration)
    {
        internal ConcurrentQueue<MimeMessage> Messages { get; } = new();

        protected override Task SendMessageAsync(MimeMessage message)
        {
            Messages.Enqueue(message);
            return Task.CompletedTask;
        }
    }
}
