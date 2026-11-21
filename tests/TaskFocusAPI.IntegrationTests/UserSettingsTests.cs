using System.Net;
using System.Net.Http.Json;
using TaskFocusAPI.Library.Models;

namespace TaskFocusAPI.IntegrationTests
{
    [Collection(ApiCollection.Name)]
    public sealed class UserSettingsTests(ApiFixture fixture)
    {
        private const string Settings = "/api/User/GetCurrentUserSettings";
        private const string UpdateSettings = "/api/User/Put/settings";

        private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

        [Fact]
        public async Task SettingsUpdateUsesSignedInUserWhenBodyNamesAnotherUser()
        {
            using var client = await fixture.CreateAuthenticatedClientAsync();
            using var otherUser = await fixture.CreateAuthenticatedClientAsync();
            var original = await GetSettingsAsync(client);
            var otherSettings = await GetSettingsAsync(otherUser);
            var updated = new UserSettingsModel
            {
                Id = otherSettings.Id,
                CleanUpImmediately = true,
                CleanUpDelayDays = 1,
                DeleteDelayDays = 90
            };

            using var response = await HttpClientJsonExtensions.PutAsJsonAsync(client, UpdateSettings, updated, CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(await response.Content.ReadAsStringAsync(CancellationToken));

            var saved = await GetSettingsAsync(client);
            Assert.Equal(original.Id, saved.Id);
            Assert.Equal(updated.CleanUpImmediately, saved.CleanUpImmediately);
            Assert.Equal(updated.CleanUpDelayDays, saved.CleanUpDelayDays);
            Assert.Equal(updated.DeleteDelayDays, saved.DeleteDelayDays);
            Assert.Equivalent(otherSettings, await GetSettingsAsync(otherUser));
        }

        [Theory]
        [InlineData(0, 30)]
        [InlineData(91, 30)]
        [InlineData(-1, 30)]
        [InlineData(7, 0)]
        [InlineData(7, 91)]
        [InlineData(7, -1)]
        public async Task SettingsUpdateRejectsOutOfRangeDelaysWithoutSaving(int cleanUpDelayDays, int deleteDelayDays)
        {
            using var client = await fixture.CreateAuthenticatedClientAsync();
            var original = await GetSettingsAsync(client);
            var updated = new UserSettingsModel
            {
                Id = original.Id,
                CleanUpImmediately = !original.CleanUpImmediately,
                CleanUpDelayDays = cleanUpDelayDays,
                DeleteDelayDays = deleteDelayDays
            };

            using var response = await HttpClientJsonExtensions.PutAsJsonAsync(client, UpdateSettings, updated, CancellationToken);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equivalent(original, await GetSettingsAsync(client));
        }

        private static async Task<UserSettingsModel> GetSettingsAsync(HttpClient client)
        {
            var settings = await client.GetFromJsonAsync<UserSettingsModel>(Settings, CancellationToken);
            Assert.NotNull(settings);
            return settings;
        }
    }
}
