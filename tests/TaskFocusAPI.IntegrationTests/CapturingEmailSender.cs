using System.Collections.Concurrent;
using TaskFocusAPI.Library.Models;
using TaskFocusAPI.Library.Utilities;

namespace TaskFocusAPI.IntegrationTests
{
    internal sealed class CapturingEmailSender : IEmailSender
    {
        internal ConcurrentDictionary<string, string> ConfirmationLinks { get; } = new();

        public Task SendConfirmationLinkAsync(UserModel recipientUser, string confirmationLink)
        {
            ConfirmationLinks[recipientUser.Email] = confirmationLink;
            return Task.CompletedTask;
        }

        public Task SendPasswordResetLinkAsync(UserModel recipientUser, string resetLink)
        {
            return SendEmailAsync(recipientUser, "Password reset", resetLink);
        }

        public Task Execute(string emailAuthKey, UserModel recipientUser, string subject, string message)
        {
            return SendEmailAsync(recipientUser, subject, message);
        }

        public Task SendEmailAsync(UserModel recipientUser, string subject, string message)
        {
            // No outbound mail from the test host.
            return Task.CompletedTask;
        }
    }
}
