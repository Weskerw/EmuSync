using System.Text.Json;

namespace EmuSync.Core;

/// <summary>
/// Everything that is specific to THIS machine and must not travel to the cloud:
/// the device identity, the local save folders and an offline cache of the user
/// settings so the app can start (and show the list) before Firebase answers.
///
/// The shared configuration — which emulators the user syncs, their labels, the
/// sync settings and the file index — lives in Firestore, see <see cref="CloudStore"/>.
/// </summary>
public class AppConfig
{
    /// <summary>Random id identifying this installation, used as the Firestore device document id.</summary>
    public string DeviceId { get; set; } = "";

    /// <summary>Friendly device name (defaults to the machine name), shown in the device list.</summary>
    public string DeviceName { get; set; } = Environment.MachineName;

    /// <summary>emulator key -> local save folder on this device.</summary>
    public Dictionary<string, string> LocalPaths { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Cached copy of the cloud settings, refreshed at every sign-in.</summary>
    public bool AutoSync { get; set; } = true;

    /// <summary>How often (minutes) to check the cloud for changes (0 = never).</summary>
    public int RemoteCheckMinutes { get; set; } = 15;

    /// <summary>Cached emulator list, so the window is populated while Firestore loads.</summary>
    public List<CachedEmulator> Emulators { get; set; } = new();

    public class CachedEmulator
    {
        public string Key { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Console { get; set; } = "";
        public bool Enabled { get; set; } = true;
        public DateTime? LastSyncUtc { get; set; }
    }

    public static readonly string ConfigDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EmuSync");

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    /// <summary>Folder where the Google Drive OAuth token is stored.</summary>
    public static string TokenDir => Path.Combine(ConfigDir, "token");

    /// <summary>True if a config file was saved before (i.e. this is not the first run).</summary>
    public static bool ConfigFileExists => File.Exists(ConfigPath);

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static AppConfig Load()
    {
        AppConfig config;
        try
        {
            config = File.Exists(ConfigPath)
                ? JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath)) ?? new AppConfig()
                : new AppConfig();

            // The property has a setter, so the deserializer hands back a plain
            // case-sensitive dictionary; folder lookups by emulator key must not
            // depend on how the key happened to be capitalised when it was written.
            config.LocalPaths = new Dictionary<string, string>(config.LocalPaths, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            // Corrupted config (or keys differing only in case): start from
            // scratch rather than refusing to launch.
            config = new AppConfig();
        }

        if (string.IsNullOrWhiteSpace(config.DeviceId))
        {
            config.DeviceId = Guid.NewGuid().ToString("N")[..16];
            config.Save();
        }
        return config;
    }

    public void Save()
    {
        Directory.CreateDirectory(ConfigDir);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this, JsonOptions));
    }

    /// <summary>Local folder configured on this device for an emulator, or null.</summary>
    public string? GetLocalPath(string key) =>
        LocalPaths.TryGetValue(key, out string? path) && !string.IsNullOrWhiteSpace(path) ? path : null;

    public void SetLocalPath(string key, string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) LocalPaths.Remove(key);
        else LocalPaths[key] = path;
    }

    /// <summary>Mirrors the cloud state locally so the next start has something to show.</summary>
    public void CacheFromCloud(CloudConfig cloud, IEnumerable<EmulatorProfile> profiles)
    {
        AutoSync = cloud.AutoSync;
        RemoteCheckMinutes = cloud.RemoteCheckMinutes;
        Emulators = profiles.Select(p => new CachedEmulator
        {
            Key = p.Key,
            DisplayName = p.DisplayName,
            Console = p.Console,
            Enabled = p.Enabled,
            LastSyncUtc = p.LastSyncUtc
        }).ToList();
    }

    /// <summary>Rebuilds the profile list from the cache (used before Firestore replies, or offline).</summary>
    public List<EmulatorProfile> ProfilesFromCache() =>
        Emulators.Select(e => new EmulatorProfile
        {
            Key = e.Key,
            DisplayName = e.DisplayName,
            Console = e.Console,
            Enabled = e.Enabled,
            LastSyncUtc = e.LastSyncUtc,
            LocalPath = GetLocalPath(e.Key) ?? ""
        }).ToList();
}
