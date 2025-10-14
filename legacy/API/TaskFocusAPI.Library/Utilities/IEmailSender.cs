using System.Threading.Tasks;
using TaskFocusAPI.Library.Models;

namespace TaskFocusAPI.Library.Utilities
{
    public interface IEmailSender
    {
        Task SendEmailAsync(UserModel recipientUser, string subject, string message);
        Task Execute(string emailAuthKey, UserModel recipientUser, string subject, string message);
        Task SendConfirmationLinkAsync(UserModel recipientUser, string confirmationLink);
        Task SendPasswordResetLinkAsync(UserModel recipientUser, string resetLink);
    }
}