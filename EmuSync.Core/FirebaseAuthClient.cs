using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EmuSync.Core;

/// <summary>
/// Protects the stored refresh token at rest. The default implementation is a
/// pass-through (the token lives in the user's roaming profile); Windows builds
/// install a DPAPI-backed one, Android will use the KeyStore.
/// </summary>
public interface ISecretProtector
{
    string Protect(string plainText);
    string Unprotect(string cipherText);
}

internal sealed class PassthroughProtector : ISecretProtector
{
    public string Protect(string plainText) => plainText;
    public string Unprotect(string cipherText) => cipherText;
}

/// <summary>The signed-in Firebase user and its tokens.</summary>
public class FirebaseSession
{
    public string Uid { get; set; } = "";
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";

    /// <summary>Long-lived token, the only one persisted to disk.</summary>
    [JsonIgnore]
    public string RefreshToken { get; set; } = "";

    /// <summary>Short-lived (1 h) token sent to Firestore as a bearer token.</summary>
    [JsonIgnore]
    public string IdToken { get; set; } = "";

    [JsonIgnore]
    public DateTime IdTokenExpiryUtc { get; set; }

    /// <summary>True when the account signed in through Google (so Drive can reuse the same consent).</summary>
    public bool IsGoogleAccount { get; set; }

    [JsonIgnore]
    public bool IsExpired => DateTime.UtcNow >= IdTokenExpiryUtc - TimeSpan.FromMinutes(5);
}

/// <summary>Raised when Firebase rejects a credential or a request.</summary>
public class FirebaseAuthException : Exception
{
    public string Code { get; }

    public FirebaseAuthException(string code, string message) : base(message) => Code = code;

    /// <summary>True when the stored refresh token is gone for good and a new sign-in is required.</summary>
    public bool RequiresSignIn =>
        Code is "TOKEN_EXPIRED" or "USER_DISABLED" or "USER_NOT_FOUND" or "INVALID_REFRESH_TOKEN"
             or "INVALID_GRANT_TYPE" or "MISSING_REFRESH_TOKEN" or "CREDENTIAL_TOO_OLD_LOGIN_AGAIN";
}

/// <summary>
/// Firebase Authentication over the Identity Toolkit REST API.
///
/// Deliberately dependency-free (just HttpClient + System.Text.Json) so the same
/// code runs on Windows, Linux, macOS and Android: there is no official Firebase
/// client SDK for .NET desktop, and the REST surface is stable and documented.
///
/// Two ways in, as configured in the Firebase console:
///  • email + password  → accounts:signUp / accounts:signInWithPassword
///  • Google            → accounts:signInWithIdp, fed with the Google ID token
///                        obtained during the Drive OAuth consent, so a Google
///                        user signs in once for both services.
/// </summary>
public class FirebaseAuthClient
{
    private const string IdentityBase = "https://identitytoolkit.googleapis.com/v1/accounts";
    private const string SecureTokenBase = "https://securetoken.googleapis.com/v1/token";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

    private readonly FirebaseOptions _options;
    private readonly ISecretProtector _protector;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public FirebaseAuthClient(FirebaseOptions options, ISecretProtector? protector = null)
    {
        _options = options;
        _protector = protector ?? new PassthroughProtector();
    }

    /// <summary>The current session, or null when signed out.</summary>
    public FirebaseSession? Session { get; private set; }

    public bool IsSignedIn => Session != null;

    /// <summary>Raised whenever the session changes (sign-in, sign-out, refresh).</summary>
    public event Action<FirebaseSession?>? SessionChanged;

    private static string SessionPath => Path.Combine(AppConfig.ConfigDir, "session.json");

    /// <summary>True when a previous sign-in left a refresh token on disk.</summary>
    public static bool HasStoredSession => File.Exists(SessionPath);

    // ---------------------------------------------------------------- sign-in

    /// <summary>Creates an account with email + password.</summary>
    public async Task<FirebaseSession> SignUpAsync(string email, string password, CancellationToken ct = default)
    {
        var json = await PostAsync($"{IdentityBase}:signUp", new
        {
            email,
            password,
            returnSecureToken = true
        }, ct);
        return Adopt(json, isGoogle: false);
    }

    /// <summary>Signs in with email + password.</summary>
    public async Task<FirebaseSession> SignInWithPasswordAsync(string email, string password, CancellationToken ct = default)
    {
        var json = await PostAsync($"{IdentityBase}:signInWithPassword", new
        {
            email,
            password,
            returnSecureToken = true
        }, ct);
        return Adopt(json, isGoogle: false);
    }

    /// <summary>
    /// Signs in (or links) with the Google ID token produced by the OAuth consent
    /// used for Drive. The OAuth client must live in the same Google Cloud project
    /// as the Firebase app, otherwise Firebase rejects the token's audience.
    /// </summary>
    public async Task<FirebaseSession> SignInWithGoogleAsync(string googleIdToken, CancellationToken ct = default)
    {
        var json = await PostAsync($"{IdentityBase}:signInWithIdp", new
        {
            postBody = $"id_token={googleIdToken}&providerId=google.com",
            requestUri = "http://localhost",
            returnIdpCredential = true,
            returnSecureToken = true
        }, ct);
        return Adopt(json, isGoogle: true);
    }

    /// <summary>Sends the "reset your password" email.</summary>
    public async Task SendPasswordResetAsync(string email, CancellationToken ct = default)
    {
        await PostAsync($"{IdentityBase}:sendOobCode", new
        {
            requestType = "PASSWORD_RESET",
            email
        }, ct);
    }

    /// <summary>
    /// Restores the session saved by a previous run, refreshing the ID token.
    /// Returns null when there is nothing stored or the token is no longer valid.
    /// </summary>
    public async Task<FirebaseSession?> RestoreSessionAsync(CancellationToken ct = default)
    {
        StoredSession? stored = ReadStoredSession();
        if (stored == null || string.IsNullOrEmpty(stored.RefreshToken)) return null;

        Session = new FirebaseSession
        {
            Uid = stored.Uid,
            Email = stored.Email,
            DisplayName = stored.DisplayName,
            IsGoogleAccount = stored.IsGoogleAccount,
            RefreshToken = stored.RefreshToken,
            IdTokenExpiryUtc = DateTime.MinValue
        };

        try
        {
            await GetIdTokenAsync(ct);
            SessionChanged?.Invoke(Session);
            return Session;
        }
        catch (FirebaseAuthException ex) when (ex.RequiresSignIn)
        {
            SignOut();
            return null;
        }
    }

    /// <summary>
    /// Returns a valid ID token, refreshing it when it is about to expire.
    /// Every Firestore call goes through here.
    /// </summary>
    public async Task<string> GetIdTokenAsync(CancellationToken ct = default)
    {
        var session = Session ?? throw new FirebaseAuthException("NOT_SIGNED_IN", "Not signed in to EmuSync.");
        if (!session.IsExpired) return session.IdToken;

        await _refreshLock.WaitAsync(ct);
        try
        {
            if (!session.IsExpired) return session.IdToken;

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = session.RefreshToken
            });

            using var response = await Http.PostAsync($"{SecureTokenBase}?key={_options.ApiKey}", content, ct);
            string body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode) throw ParseError(body);

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            session.IdToken = root.GetProperty("id_token").GetString() ?? "";
            session.RefreshToken = root.GetProperty("refresh_token").GetString() ?? session.RefreshToken;
            session.Uid = root.TryGetProperty("user_id", out var uid) ? uid.GetString() ?? session.Uid : session.Uid;
            session.IdTokenExpiryUtc = DateTime.UtcNow.AddSeconds(ParseExpiry(root));

            SaveSession(session);
            SessionChanged?.Invoke(session);
            return session.IdToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <summary>Forgets the session: the next start asks for credentials again.</summary>
    public void SignOut()
    {
        Session = null;
        try { if (File.Exists(SessionPath)) File.Delete(SessionPath); }
        catch (IOException) { /* best effort */ }
        catch (UnauthorizedAccessException) { /* best effort */ }
        SessionChanged?.Invoke(null);
    }

    // ---------------------------------------------------------------- plumbing

    private async Task<JsonElement> PostAsync(string url, object payload, CancellationToken ct)
    {
        _options.EnsureConfigured();

        using var response = await Http.PostAsJsonAsync($"{url}?key={_options.ApiKey}", payload, ct);
        string body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) throw ParseError(body);

        return JsonDocument.Parse(body).RootElement.Clone();
    }

    private FirebaseSession Adopt(JsonElement json, bool isGoogle)
    {
        var session = new FirebaseSession
        {
            Uid = Str(json, "localId"),
            Email = Str(json, "email"),
            DisplayName = Str(json, "displayName"),
            IdToken = Str(json, "idToken"),
            RefreshToken = Str(json, "refreshToken"),
            IsGoogleAccount = isGoogle,
            IdTokenExpiryUtc = DateTime.UtcNow.AddSeconds(ParseExpiry(json))
        };

        Session = session;
        SaveSession(session);
        SessionChanged?.Invoke(session);
        return session;
    }

    private static string Str(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) ? v.GetString() ?? "" : "";

    private static int ParseExpiry(JsonElement el)
    {
        foreach (string name in new[] { "expiresIn", "expires_in" })
        {
            if (!el.TryGetProperty(name, out var v)) continue;
            if (v.ValueKind == JsonValueKind.Number) return v.GetInt32();
            if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out int parsed)) return parsed;
        }
        return 3600;
    }

    /// <summary>Turns an Identity Toolkit error payload into a message a user can act on.</summary>
    private static FirebaseAuthException ParseError(string body)
    {
        string code = "UNKNOWN";
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.String)
                    code = error.GetString() ?? code;                       // secure token endpoint
                else if (error.TryGetProperty("message", out var msg))
                    code = msg.GetString() ?? code;                          // identity toolkit
            }
        }
        catch { /* keep UNKNOWN */ }

        // Codes can carry a suffix, e.g. "WEAK_PASSWORD : Password should be at least 6 characters".
        string bare = code.Split(':')[0].Trim();

        string message = bare switch
        {
            "EMAIL_EXISTS" => "An account with this email already exists.",
            "EMAIL_NOT_FOUND" => "No account exists with this email.",
            "INVALID_PASSWORD" => "Wrong password.",
            "INVALID_LOGIN_CREDENTIALS" => "Wrong email or password.",
            "INVALID_EMAIL" => "The email address is not valid.",
            "MISSING_PASSWORD" => "Enter a password.",
            "WEAK_PASSWORD" => "The password must be at least 6 characters long.",
            "USER_DISABLED" => "This account has been disabled.",
            "TOO_MANY_ATTEMPTS_TRY_LATER" => "Too many attempts. Try again in a few minutes.",
            "TOKEN_EXPIRED" or "INVALID_REFRESH_TOKEN" => "The session has expired: please sign in again.",
            "OPERATION_NOT_ALLOWED" => "This sign-in method is disabled in the Firebase project.",
            "CONFIGURATION_NOT_FOUND" => "Firebase Authentication is not enabled for this project.",
            _ => $"Firebase error: {code}"
        };

        return new FirebaseAuthException(bare, message);
    }

    // ---------------------------------------------------------------- storage

    /// <summary>What actually goes on disk: everything but the short-lived ID token.</summary>
    private sealed class StoredSession
    {
        public string Uid { get; set; } = "";
        public string Email { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public bool IsGoogleAccount { get; set; }
        public string RefreshToken { get; set; } = "";
    }

    private void SaveSession(FirebaseSession session)
    {
        try
        {
            Directory.CreateDirectory(AppConfig.ConfigDir);
            var stored = new StoredSession
            {
                Uid = session.Uid,
                Email = session.Email,
                DisplayName = session.DisplayName,
                IsGoogleAccount = session.IsGoogleAccount,
                RefreshToken = _protector.Protect(session.RefreshToken)
            };
            File.WriteAllText(SessionPath, JsonSerializer.Serialize(stored, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Not fatal: the user will simply have to sign in again next time.
        }
    }

    private StoredSession? ReadStoredSession()
    {
        try
        {
            if (!File.Exists(SessionPath)) return null;
            var stored = JsonSerializer.Deserialize<StoredSession>(File.ReadAllText(SessionPath));
            if (stored == null) return null;
            stored.RefreshToken = _protector.Unprotect(stored.RefreshToken);
            return stored;
        }
        catch
        {
            return null;
        }
    }
}
