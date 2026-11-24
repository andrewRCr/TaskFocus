using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace TaskFocusWeb.Authentication;

internal static class SignInRedirect
{
    public static string? GetReturnUrl(NavigationManager navigation)
    {
        var (returnUrl, confirmationEmail) = GetLoginReturnUrl(navigation);
        return confirmationEmail is null ? returnUrl : null;
    }

    public static (string? ReturnUrl, string? ConfirmationEmail) GetLoginReturnUrl(NavigationManager navigation)
    {
        var uri = navigation.ToAbsoluteUri(navigation.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);
        if (!query.TryGetValue("returnUrl", out var values) || values.Count != 1)
        {
            return (null, null);
        }

        var returnUrl = ValidateReturnUrl(navigation, values[0]);
        var isConfirmationPage = uri.AbsolutePath.Trim('/').Equals("unconfirmedemail", StringComparison.OrdinalIgnoreCase);
        if (!query.TryGetValue(isConfirmationPage ? "email" : "confirmationEmail", out var emails))
        {
            return (isConfirmationPage ? null : returnUrl, null);
        }

        return emails.Count == 1 && !string.IsNullOrWhiteSpace(emails[0])
            ? (returnUrl, emails[0])
            : (null, null);
    }

    public static string? ValidateReturnUrl(NavigationManager navigation, string? returnUrl)
    {
        // A rooted path must not have an authority or contain characters the browser can normalize into one.
        if (returnUrl is not ['/', ..] || returnUrl.StartsWith("//", StringComparison.Ordinal)
            || returnUrl.Contains('\\') || returnUrl.Any(char.IsControl))
        {
            return null;
        }

        var path = Uri.UnescapeDataString(navigation.ToAbsoluteUri(returnUrl).AbsolutePath);
        if (path.StartsWith("//", StringComparison.Ordinal) || path.Contains('\\'))
        {
            return null;
        }

        return path.Trim('/').ToLowerInvariant() is "logout" or "attemptlogin" or "autherror"
            or "unconfirmedemail" or "unconfirmedupdatedemail" or "resetpw" ? null : returnUrl;
    }

    public static string LoginUrl(string? returnUrl, string? confirmationEmail = null, string? prefillEmail = null)
    {
        var loginUrl = "/";
        if (returnUrl is not null)
        {
            loginUrl = QueryHelpers.AddQueryString(loginUrl, "returnUrl", returnUrl);
            if (confirmationEmail is not null)
            {
                loginUrl = QueryHelpers.AddQueryString(loginUrl, "confirmationEmail", confirmationEmail);
            }
        }

        return prefillEmail is null ? loginUrl : QueryHelpers.AddQueryString(loginUrl, "prefillEmail", prefillEmail);
    }

    // Email links can open in a new tab without the return URL in their query.
    public static string ConfirmationStorageKey(string email) => $"emailConfirmationReturnUrl:{email.Trim().ToUpperInvariant()}";
}
