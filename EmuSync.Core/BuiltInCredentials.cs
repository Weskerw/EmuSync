namespace EmuSync.Core;

/// <summary>
/// OAuth credentials embedded in the app, so end users only have to sign in
/// with Google, without creating anything in the Cloud Console.
///
/// AS THE DEVELOPER (one time only): create the "Desktop app" OAuth client ID
/// as described in the README, open the downloaded JSON and copy client_id
/// and client_secret here.
/// Note: for desktop apps Google does not treat the client_secret as
/// confidential; embedding it in the distributed executable is standard practice.
///
/// ⚠ PUBLIC REPO: NEVER commit real values! This file must stay in the repo
/// with empty strings. Fill in the values locally only when building a release.
/// For day-to-day development use a credentials.json next to the .csproj
/// instead: it is already in .gitignore and takes precedence over this class.
/// </summary>
public static class BuiltInCredentials
{
    public const string ClientId = "";     // e.g. "1234567890-abc123.apps.googleusercontent.com"
    public const string ClientSecret = ""; // e.g. "GOCSPX-..."

    public static bool Available =>
        !string.IsNullOrEmpty(ClientId) && !string.IsNullOrEmpty(ClientSecret);
}
