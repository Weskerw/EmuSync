using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Http;
using Google.Apis.Util.Store;

namespace EmuSync.Core;

/// <summary>Result of a Google authorization: the Drive credential plus the identity tokens.</summary>
public class GoogleAuthResult
{
    /// <summary>Credential to hand to the Drive service.</summary>
    public IConfigurableHttpClientInitializer Credential { get; init; } = null!;

    /// <summary>
    /// Google OpenID Connect ID token. Passed to
    /// <see cref="FirebaseAuthClient.SignInWithGoogleAsync"/> so one consent
    /// covers both Drive and the EmuSync account.
    /// </summary>
    public string? IdToken { get; init; }
}

/// <summary>
/// Obtains Drive authorization. Abstracted because the consent flow is the only
/// genuinely platform-specific piece of EmuSync: desktop spins up a loopback
/// listener and opens the system browser, while Android needs Custom Tabs /
/// AppAuth. Everything else in EmuSync.Core runs unchanged on both.
/// </summary>
public interface IGoogleAuthorizationProvider
{
    /// <summary>True when a previous consent left a refresh token on disk (no browser needed).</summary>
    bool HasStoredToken { get; }

    /// <summary>Authorizes, reusing the stored token when possible.</summary>
    Task<GoogleAuthResult> AuthorizeAsync(CancellationToken ct = default);

    /// <summary>Drops the stored token and runs the consent flow from scratch.</summary>
    Task<GoogleAuthResult> ReauthorizeAsync(CancellationToken ct = default);

    /// <summary>Forgets the stored token.</summary>
    void SignOut();
}

/// <summary>
/// Desktop implementation (Windows, Linux, macOS): opens the system browser on a
/// loopback redirect and caches the refresh token under %APPDATA%\EmuSync\token.
///
/// The OAuth client must be a "Desktop app" client created in the SAME Google
/// Cloud project as the Firebase app: Firebase only accepts ID tokens whose
/// audience is one of the project's own OAuth clients.
/// </summary>
public class DesktopGoogleAuthProvider : IGoogleAuthorizationProvider
{
    /// <summary>
    /// Drive access plus the OpenID scopes: without 'openid' Google does not
    /// return an ID token and the Firebase sign-in could not be chained to this
    /// same consent.
    /// </summary>
    private static readonly string[] Scopes =
    {
        DriveService.Scope.DriveFile,
        "openid",
        "email",
        "profile"
    };

    private readonly string? _credentialsPath;

    public DesktopGoogleAuthProvider(string? credentialsPath = null) => _credentialsPath = credentialsPath;

    public bool HasStoredToken =>
        Directory.Exists(AppConfig.TokenDir) && Directory.EnumerateFiles(AppConfig.TokenDir).Any();

    public async Task<GoogleAuthResult> AuthorizeAsync(CancellationToken ct = default)
    {
        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            LoadSecrets(_credentialsPath),
            Scopes,
            "user",
            ct,
            new FileDataStore(AppConfig.TokenDir, true));

        // The ID token comes with the access token; refresh it if it is stale so
        // the Firebase sign-in always gets a fresh one.
        if (credential.Token.IsStale)
            await credential.RefreshTokenAsync(ct);

        return new GoogleAuthResult
        {
            Credential = credential,
            IdToken = credential.Token.IdToken
        };
    }

    public Task<GoogleAuthResult> ReauthorizeAsync(CancellationToken ct = default)
    {
        SignOut();
        return AuthorizeAsync(ct);
    }

    public void SignOut()
    {
        try
        {
            if (Directory.Exists(AppConfig.TokenDir))
                Directory.Delete(AppConfig.TokenDir, true);
        }
        catch (IOException) { /* the token will simply be overwritten */ }
        catch (UnauthorizedAccessException) { /* idem */ }
    }

    private static ClientSecrets LoadSecrets(string? credentialsPath)
    {
        if (credentialsPath != null && File.Exists(credentialsPath))
        {
            using var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read);
            return GoogleClientSecrets.FromStream(stream).Secrets;
        }
        if (BuiltInCredentials.Available)
        {
            return new ClientSecrets
            {
                ClientId = BuiltInCredentials.ClientId,
                ClientSecret = BuiltInCredentials.ClientSecret
            };
        }
        throw new InvalidOperationException(
            "No OAuth credentials available.\n" +
            "Fill in BuiltInCredentials.cs (see README) or place a " +
            "credentials.json next to the executable.");
    }
}
