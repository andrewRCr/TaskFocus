using System.Net;
using System.Net.Http.Json;
using TaskFocusAPI.Library.Models;
using TaskFocusAPI.Models;

namespace TaskFocusAPI.IntegrationTests
{
    [Collection(ApiCollection.Name)]
    public sealed class UserProfileTests(ApiFixture fixture)
    {
        [Fact]
        public async Task NameUpdateReturnsInternalServerErrorWhenProfileIsMissing()
        {
            var user = await fixture.CreateUserAsync(createProfile: false);
            using var client = await fixture.CreateAuthenticatedClientAsync(user);
            var updated = new ApplicationUserModel
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = "Test",
                LastName = "User"
            };

            using var response = await HttpClientJsonExtensions.PutAsJsonAsync(client, "/api/User/UpdateName", updated,
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task NameUpdateSavesNamesAndSyncMetadataWithoutChangingEmails()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            string profileEmail = ApiFixture.NewEmail();
            var user = await fixture.CreateUserAsync(profileEmail: profileEmail);
            using var client = await fixture.CreateAuthenticatedClientAsync(user);
            var updated = new ApplicationUserModel
            {
                Id = user.Id,
                Email = ApiFixture.NewEmail(),
                FirstName = "New first name",
                LastName = "New last name",
                ClientLastUpdated = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero),
                ServerLastUpdated = new DateTimeOffset(2026, 1, 1, 10, 1, 0, TimeSpan.Zero)
            };

            using var response = await HttpClientJsonExtensions.PutAsJsonAsync(client, "/api/User/UpdateName", updated, cancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(await response.Content.ReadAsStringAsync(cancellationToken));

            var profile = await client.GetFromJsonAsync<UserModel>("/api/User/GetCurrentUser", cancellationToken);
            Assert.NotNull(profile);
            Assert.Equal(updated.FirstName, profile.FirstName);
            Assert.Equal(updated.LastName, profile.LastName);
            Assert.Equal(updated.ClientLastUpdated, profile.ClientLastUpdated);
            Assert.Equal(updated.ServerLastUpdated, profile.ServerLastUpdated);
            Assert.Equal(profileEmail, profile.Email);

            var identity = await fixture.FindIdentityUserAsync(user.Id);
            Assert.NotNull(identity);
            Assert.Equal(user.Email, identity.Email);
        }
    }
}
