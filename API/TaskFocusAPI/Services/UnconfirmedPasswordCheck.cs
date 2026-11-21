using Microsoft.AspNetCore.Identity;

namespace TaskFocusAPI.Services
{
    internal static class UnconfirmedPasswordCheck
    {
        internal static async Task<bool> CheckAsync(UserManager<IdentityUser> users, IdentityUser user, string password)
        {
            // Identity stops at the confirmation requirement before checking the password or counting a failure.
            if (await users.IsLockedOutAsync(user)) { return false; }

            if (!string.IsNullOrEmpty(password) && await users.CheckPasswordAsync(user, password))
            {
                return true;
            }

            await users.AccessFailedAsync(user);
            return false;
        }
    }
}
