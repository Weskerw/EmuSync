using System.Text.Json;
using System.Text.Json.Serialization;

namespace EmuSync.Core;

/// <summary>
/// Firebase project settings. These are public values (the same ones a web app
/// ships in its <c>firebaseConfig</c>): access control is enforced by the
/// Firestore security rules, not by keeping the API key secret.
///
/// AS THE DEVELOPER (one time only): create the Firebase project, add a Web app
/// and copy <c>apiKey</c> and <c>projectId</c> into <see cref="EmbeddedApiKey"/>
/// / <see cref="EmbeddedProjectId"/>, or ship an <c>emusync-firebase.json</c>
/// next to the executable:
/// <code>{ "apiKey": "AIza...", "projectId": "emusync-xxxxx" }</code>
/// The file takes precedence over the embedded values, which makes it easy to
/// point a dev build at a staging project. (The name is deliberately not
/// <c>firebase.json</c>, which belongs to the Firebase CLI in <c>site/</c>.)
/// </summary>
public class FirebaseOptions
{
    public const string FileName = "emusync-firebase.json";

    // The project id is public (it is already in site/.firebaserc); the API key
    // is public too — Firebase web apps ship it in plain sight — but it is left
    // empty here so forks point at their own project by default.
    private const string EmbeddedApiKey = "";
    private const string EmbeddedProjectId = "emusync-43d2b";

    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = "";

    [JsonPropertyName("projectId")]
    public string ProjectId { get; set; } = "";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(ProjectId);

    /// <summary>Base URL of the Firestore REST API for this project's default database.</summary>
    public string FirestoreBaseUrl =>
        $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents";

    /// <summary>
    /// Loads the settings from <c>emusync-firebase.json</c> next to the executable,
    /// falling back to the values embedded at build time.
    /// </summary>
    public static FirebaseOptions Load(string? configPath = null)
    {
        configPath ??= Path.Combine(AppContext.BaseDirectory, FileName);
        try
        {
            if (File.Exists(configPath))
            {
                var fromFile = JsonSerializer.Deserialize<FirebaseOptions>(File.ReadAllText(configPath));
                if (fromFile is { IsConfigured: true }) return fromFile;
            }
        }
        catch
        {
            // Malformed file: fall back to the embedded values.
        }

        return new FirebaseOptions { ApiKey = EmbeddedApiKey, ProjectId = EmbeddedProjectId };
    }

    public void EnsureConfigured()
    {
        if (!IsConfigured)
            throw new InvalidOperationException(
                "Firebase is not configured.\n" +
                $"Fill in FirebaseOptions (apiKey / projectId) or place an {FileName} " +
                "next to the executable. See the README.");
    }
}
