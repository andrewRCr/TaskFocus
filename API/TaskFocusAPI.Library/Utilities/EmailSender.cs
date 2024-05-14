using MailKit.Net.Smtp;
using MimeKit;
using TaskFocusAPI.Library.Models;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using MimeKit.Text;

namespace TaskFocusAPI.Library.Utilities
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _config;

        public EmailSender(IConfiguration config)
        {
            _config = config;
        }

        public Task SendConfirmationLinkAsync(UserModel recipientUser,
            string confirmationLink) => SendEmailAsync(recipientUser, "Confirm your email",
            $"Please confirm your account by " + $"<a href='{confirmationLink}'>clicking here</a>.");

        public Task SendPasswordResetLinkAsync(UserModel recipientUser,
            string resetLink) => SendEmailAsync(recipientUser, "Reset your password",
            $"Please reset your password by <a href='{resetLink}'>clicking here</a>.");

        public async Task SendEmailAsync(UserModel recipientUser, string subject, string message)
        {
            string emailAuthKey = null;
            string keyVaultUrl = _config["AzureKeyVaultUrl"];
            var secretsClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
            emailAuthKey = secretsClient.GetSecret("ZeptoMailAuthKey").Value.Value;

            if (string.IsNullOrWhiteSpace(emailAuthKey)) {  throw new Exception("Null emailAuthKey!"); }

            await Execute(emailAuthKey, recipientUser, subject, message);
        }

        public async Task Execute(string emailAuthKey, UserModel recipientUser, string subject, string message)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("TaskFocus", "taskfocus@andrewcreekmore.com"));
            string recipientFullName = recipientUser.FirstName + " " + recipientUser.LastName;
            email.To.Add(new MailboxAddress(recipientFullName, recipientUser.Email));
            email.Subject = subject;

            email.Body = new TextPart("html") { Text = message };

            using (var client = new SmtpClient())
            {
                client.Connect("smtp.zeptomail.com", 465, true);
                client.Authenticate("emailapikey", emailAuthKey);

                await client.SendAsync(email);
                client.Disconnect(true);
            }
        }
    }
}
