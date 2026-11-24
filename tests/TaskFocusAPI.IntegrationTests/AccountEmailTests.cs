using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;
using MimeKit;
using TaskFocusAPI.Library.Models;
using TaskFocusAPI.Models;

namespace TaskFocusAPI.IntegrationTests
{
    [Collection(ApiCollection.Name)]
    public sealed class AccountEmailTests(ApiFixture fixture)
    {
        private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

        [Fact]
        public async Task RegistrationSendsUsableConfirmationLinkForPlusAddress()
        {
            using var client = fixture.CreateClient();
            string email = NewPlusEmail();

            using var registration = await HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/User/Register",
                new UserRegistrationModel("Test", "User", email, ApiFixture.Password), CancellationToken);
            Assert.Equal(HttpStatusCode.OK, registration.StatusCode);

            var link = ReadAccountLink(fixture.Emails.Messages.Last(), "/unconfirmedemail", email, email);
            await ConfirmEmailAsync(client, link);
        }

        [Fact]
        public async Task ResendUsesStoredIdentitySpellingForRecipientAndLink()
        {
            string email = NewPlusEmail();
            await fixture.CreateUserAsync(confirmed: false, email: email);
            using var client = fixture.CreateClient();

            using var resend = await HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/User/SendEmailConfirmationLink",
                new UserModel { Email = email.ToUpperInvariant() }, CancellationToken);
            Assert.Equal(HttpStatusCode.OK, resend.StatusCode);

            var link = ReadAccountLink(fixture.Emails.Messages.Last(), "/unconfirmedemail", email, email);
            await ConfirmEmailAsync(client, link);
        }

        [Fact]
        public async Task LegacyConfirmationLinkWithSpacesInTokenStillConfirmsEmail()
        {
            string email = ApiFixture.NewEmail();
            await fixture.CreateUserAsync(confirmed: false, email: email);
            using var client = fixture.CreateClient();

            using var resend = await HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/User/SendEmailConfirmationLink",
                new UserModel { Email = email }, CancellationToken);
            Assert.Equal(HttpStatusCode.OK, resend.StatusCode);

            var query = QueryHelpers.ParseQuery($"?email={email}&token={fixture.Tokens.GeneratedTokens[email]}");
            string token = Assert.IsType<string>(Assert.Single(query["token"]));
            Assert.Contains(' ', token);
            await ConfirmEmailAsync(client, new ConfirmEmailModel { Email = query["email"].ToString(), Token = token });
        }

        [Fact]
        public async Task PasswordResetSendsUsableLinkOnlyToIdentityAddress()
        {
            string email = NewPlusEmail();
            await fixture.CreateUserAsync(email: email, profileEmail: ApiFixture.NewEmail());
            using var client = fixture.CreateClient();
            int previousMessageCount = fixture.Emails.Messages.Count;

            using var request = await HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/User/SendPasswordResetEmail",
                new UserModel { Email = email }, CancellationToken);
            Assert.Equal(HttpStatusCode.OK, request.StatusCode);

            var message = Assert.Single(fixture.Emails.Messages.Skip(previousMessageCount));
            var link = ReadAccountLink(message, "/resetpw", email, email);
            await ResetPasswordAndSignInAsync(client, link);
        }

        [Fact]
        public async Task PasswordChangeNoticeGoesOnlyToIdentityAddress()
        {
            string email = NewPlusEmail();
            string profileEmail = ApiFixture.NewEmail();
            var user = await fixture.CreateUserAsync(email: email, profileEmail: profileEmail);
            using var client = await fixture.CreateAuthenticatedClientAsync(user);
            int previousMessageCount = fixture.Emails.Messages.Count;

            using var update = await HttpClientJsonExtensions.PutAsJsonAsync(client, "/api/User/UpdatePassword?reportRejection=true",
                new UserRegistrationModel("Test", "User", profileEmail, "New-fixture-password!123"), CancellationToken);
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);
            Assert.Empty(await update.Content.ReadAsStringAsync(CancellationToken));

            var message = Assert.Single(fixture.Emails.Messages.Skip(previousMessageCount));
            AssertPasswordChangeNotice(message, email);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task LegacyPasswordChangeNoticeRequestSucceedsWithoutSendingMail(bool accountExists)
        {
            string email = NewPlusEmail();
            if (accountExists)
            {
                await fixture.CreateUserAsync(email: email);
            }
            using var client = fixture.CreateClient();
            int previousMessageCount = fixture.Emails.Messages.Count;

            using var notice = await HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/User/SendPasswordChangeSuccessEmail",
                new UserModel { Email = email.ToUpperInvariant() }, CancellationToken);
            Assert.Equal(HttpStatusCode.OK, notice.StatusCode);
            var body = await notice.Content.ReadFromJsonAsync<Response>(CancellationToken);
            Assert.Equal("Success", body?.Status);
            Assert.Equal($"Password changed successfully email sent to {(accountExists ? email : email.ToUpperInvariant())}.", body?.Message);
            Assert.Equal(previousMessageCount, fixture.Emails.Messages.Count);
        }

        [Fact]
        public async Task LegacyPasswordChangeNoticeRequestSucceedsWithoutAnAddress()
        {
            using var client = fixture.CreateClient();
            int previousMessageCount = fixture.Emails.Messages.Count;

            using var notice = await HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/User/SendPasswordChangeSuccessEmail",
                new { }, CancellationToken);

            Assert.Equal(HttpStatusCode.OK, notice.StatusCode);
            var body = await notice.Content.ReadFromJsonAsync<Response>(CancellationToken);
            Assert.Equal("Success", body?.Status);
            Assert.Equal("Password changed successfully email sent to .", body?.Message);
            Assert.Equal(previousMessageCount, fixture.Emails.Messages.Count);
        }

        [Fact]
        public async Task PasswordRecoveryWorksWithoutAProfile()
        {
            string email = NewPlusEmail();
            await fixture.CreateUserAsync(email: email, createProfile: false);
            using var client = fixture.CreateClient();
            int previousMessageCount = fixture.Emails.Messages.Count;

            using var request = await HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/User/SendPasswordResetEmail",
                new UserModel { Email = email }, CancellationToken);
            Assert.Equal(HttpStatusCode.OK, request.StatusCode);

            var resetMessage = Assert.Single(fixture.Emails.Messages.Skip(previousMessageCount));
            var link = ReadAccountLink(resetMessage, "/resetpw", email, email);
            Assert.StartsWith("Hello,", Assert.IsType<string>(resetMessage.HtmlBody));
            await ResetPasswordAndSignInAsync(client, link);

            var noticeMessage = Assert.Single(fixture.Emails.Messages.Skip(previousMessageCount + 1));
            AssertPasswordChangeNotice(noticeMessage, email);
            Assert.StartsWith("Hello,", Assert.IsType<string>(noticeMessage.HtmlBody));
        }

        [Fact]
        public async Task AccountLinksKeepStartupOriginAfterConfigurationReload()
        {
            string email = NewPlusEmail();
            await fixture.CreateUserAsync(email: email);
            using var client = fixture.CreateClient();
            var configuration = fixture.Configuration;
            var provider = configuration.Providers.Last(provider => provider.TryGet("AppUrl", out _));
            string? startupUrl = configuration["AppUrl"];
            const string reloadedUrl = "http://taskfocus.example.invalid";

            try
            {
                provider.Set("AppUrl", reloadedUrl);
                configuration.Reload();
                Assert.Equal(reloadedUrl, configuration["AppUrl"]);

                using var request = await HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/User/SendPasswordResetEmail",
                    new UserModel { Email = email }, CancellationToken);
                Assert.Equal(HttpStatusCode.OK, request.StatusCode);

                ReadAccountLink(fixture.Emails.Messages.Last(), "/resetpw", email, email);
            }
            finally
            {
                provider.Set("AppUrl", startupUrl);
                configuration.Reload();
            }
        }

        private static async Task ResetPasswordAndSignInAsync(HttpClient client, ConfirmEmailModel link)
        {
            const string newPassword = "New-fixture-password!123";
            using var reset = await HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/User/ResetPassword", new ResetPasswordModel
            {
                Email = link.Email,
                Token = link.Token,
                Password = newPassword,
                ConfirmPassword = newPassword
            }, CancellationToken);
            Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

            using var signIn = await ApiFixture.RequestTokenAsync(client, link.Email, newPassword);
            Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);
        }

        private static void AssertPasswordChangeNotice(MimeMessage message, string email)
        {
            Assert.Equal(email, Assert.Single(message.To.Mailboxes).Address);
            Assert.Empty(message.Cc);
            Assert.Empty(message.Bcc);
            Assert.Equal("Password changed", message.Subject);
        }

        [Fact]
        public async Task EmailChangeSendsLinkWithExactNewAddressAndToken()
        {
            var user = await fixture.CreateUserAsync();
            string newEmail = NewPlusEmail();
            using var client = await fixture.CreateAuthenticatedClientAsync(user);

            using var request = await HttpClientJsonExtensions.PutAsJsonAsync(client, "/api/User/RequestUpdateEmail",
                new ApplicationUserModel { Id = user.Id, Email = newEmail }, CancellationToken);
            Assert.Equal(HttpStatusCode.OK, request.StatusCode);

            ReadAccountLink(fixture.Emails.Messages.Last(), "/unconfirmedupdatedemail", newEmail, user.Email!);
        }

        private ConfirmEmailModel ReadAccountLink(MimeMessage message, string path, string recipient, string identityEmail)
        {
            var sender = Assert.Single(message.From.Mailboxes);
            Assert.Equal(TaskFocusApiFactory.FromAddress, sender.Address);
            Assert.Equal(TaskFocusApiFactory.FromName, sender.Name);
            Assert.Equal(recipient, Assert.Single(message.To.Mailboxes).Address);
            Assert.Empty(message.Cc);
            Assert.Empty(message.Bcc);

            string html = Assert.IsType<string>(message.HtmlBody);
            var href = Regex.Match(html, "href='([^']*)'");
            Assert.True(href.Success);
            string encodedUrl = href.Groups[1].Value;
            Assert.Contains("&amp;token=", encodedUrl);

            var url = new Uri(WebUtility.HtmlDecode(encodedUrl));
            Assert.Equal(TaskFocusApiFactory.AppUrl, url.GetLeftPart(UriPartial.Authority));
            Assert.Equal(path, url.AbsolutePath);
            var query = QueryHelpers.ParseQuery(url.Query);
            string email = Assert.IsType<string>(Assert.Single(query["email"]));
            string token = Assert.IsType<string>(Assert.Single(query["token"]));
            Assert.Equal(recipient, email);
            Assert.Equal(fixture.Tokens.GeneratedTokens[identityEmail], token);
            return new ConfirmEmailModel { Email = email, Token = token };
        }

        private static async Task ConfirmEmailAsync(HttpClient client, ConfirmEmailModel link)
        {
            using var confirmation = await HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/User/ConfirmEmail", link, CancellationToken);
            Assert.Equal(HttpStatusCode.OK, confirmation.StatusCode);
        }

        private static string NewPlusEmail() => ApiFixture.NewEmail().Replace("@", "+tasks@");
    }
}
