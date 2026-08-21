using System.Text.Json;

namespace EmuSync.Core;

public class AppConfig
{
    public List<SyncProfile> Profiles { get; set; } = new();

    /// <summary>Automatically sync when local changes are detected.</summary>
    public bool AutoSync { get; set; } = true;

    /// <summary>How often (minutes) to check Drive for remote changes (0 = never).</summary>
    public int RemoteCheckMinutes { get; set; } = 15;

    private static readonly string ConfigDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EmuSync");

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    /// <summary>Folder where the Google OAuth token is stored.</summary>
    public static string TokenDir => Path.Combine(ConfigDir, "token");

    /// <summary>True if a config file was saved before (i.e. this is not the first run).</summary>
    public static bool ConfigFileExists => File.Exists(ConfigPath);

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
                return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath)) ?? new AppConfig();
        }
        catch
        {
            // Corrupted config: start from scratch.
        }
        return new AppConfig();
    }

    public void Save()
    {
        Directory.CreateDirectory(ConfigDir);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this, JsonOptions));
    }
}
