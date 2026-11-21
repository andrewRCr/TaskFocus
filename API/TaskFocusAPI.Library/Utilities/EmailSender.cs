using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using TaskFocusAPI.Library.Models;

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
            $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(confirmationLink)}'>clicking here</a>.");

        public Task SendPasswordResetLinkAsync(UserModel recipientUser,
            string resetLink) => SendEmailAsync(recipientUser, "Reset your password",
            $"Hello,<br><br>Please reset your password by <a href='{HtmlEncoder.Default.Encode(resetLink)}'>clicking here</a>.");

        public Task SendEmailAsync(UserModel recipientUser, string subject, string message)
        {
            return SendMessageAsync(CreateMessage(recipientUser, subject, message));
        }

        protected virtual async Task SendMessageAsync(MimeMessage message)
        {
            string emailAuthKey = null;
            string keyVaultUrl = _config["AzureKeyVaultUrl"];
            var secretsClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
            emailAuthKey = secretsClient.GetSecret("ZeptoMailAuthKey").Value.Value;

            if (string.IsNullOrWhiteSpace(emailAuthKey)) {  throw new Exception("Null emailAuthKey!"); }

            await Execute(emailAuthKey, message);
        }

        public Task Execute(string emailAuthKey, UserModel recipientUser, string subject, string message)
        {
            return Execute(emailAuthKey, CreateMessage(recipientUser, subject, message));
        }

        private MimeMessage CreateMessage(UserModel recipientUser, string subject, string message)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_config["Email:FromName"], _config["Email:FromAddress"]));
            string recipientFullName = (recipientUser.FirstName + " " + recipientUser.LastName).Trim();
            email.To.Add(new MailboxAddress(recipientFullName, recipientUser.Email));
            email.Subject = subject;

            email.Body = new TextPart("html") { Text = message };
            return email;
        }

        private static async Task Execute(string emailAuthKey, MimeMessage email)
        {
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
