using System.Text.Json;

namespace EmuSync.Core;

/// <summary>One file as this device last saw it in sync.</summary>
public class SnapshotEntry
{
    public string Md5 { get; set; } = "";
    public long Size { get; set; }
    public DateTime ModifiedUtc { get; set; }
}

/// <summary>What this device had in sync for one emulator, at the end of its last run.</summary>
public class DeviceSnapshot
{
    public string EmulatorKey { get; set; } = "";
    public DateTime? TakenUtc { get; set; }

    /// <summary>
    /// Drive folder the snapshot was taken against. If the user later points
    /// EmuSync at a different folder, the snapshot describes somewhere else and
    /// must be ignored rather than acted on.
    /// </summary>
    public string RootPath { get; set; } = "";

    /// <summary>relative path ('/' separator) -> state at the last sync</summary>
    public Dictionary<string, SnapshotEntry> Files { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Per-device snapshots of the last successful sync, kept locally under
/// <c>%APPDATA%\EmuSync\index</c>.
///
/// This is the missing third state a two-way sync needs. The shared Firestore
/// index cannot play that role: it is rewritten by whichever device synced last,
/// so "in the index but missing here" would mean "another device just uploaded
/// it" as often as "I deleted it" — and acting on that would trash brand-new
/// files. A snapshot that belongs to this machine alone answers the question
/// unambiguously: if a file is in MY snapshot and gone from MY disk, I deleted it.
/// </summary>
public static class DeviceIndexStore
{
    private static string IndexDir => Path.Combine(AppConfig.ConfigDir, "index");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private static string PathFor(string emulatorKey) =>
        Path.Combine(IndexDir, EmulatorCatalog.MakeKey(emulatorKey) + ".json");

    public static DeviceSnapshot Load(string emulatorKey)
    {
        try
        {
            string path = PathFor(emulatorKey);
            if (File.Exists(path))
            {
                var snapshot = JsonSerializer.Deserialize<DeviceSnapshot>(File.ReadAllText(path));
                if (snapshot != null)
                {
                    snapshot.EmulatorKey = emulatorKey;

                    // The property has a setter, so the deserializer replaces the
                    // dictionary with a case-SENSITIVE one. Everything it is
                    // compared against is case-insensitive, and a mismatch would
                    // silently disable deletion detection.
                    snapshot.Files = new Dictionary<string, SnapshotEntry>(
                        snapshot.Files, StringComparer.OrdinalIgnoreCase);
                    return snapshot;
                }
            }
        }
        catch
        {
            // A corrupted snapshot is not worth failing over: without it the sync
            // simply falls back to "never delete anything", which is the safe side.
        }

        return new DeviceSnapshot { EmulatorKey = emulatorKey };
    }

    public static void Save(DeviceSnapshot snapshot)
    {
        try
        {
            Directory.CreateDirectory(IndexDir);
            snapshot.TakenUtc = DateTime.UtcNow;
            File.WriteAllText(PathFor(snapshot.EmulatorKey), JsonSerializer.Serialize(snapshot, JsonOptions));
        }
        catch
        {
            // Losing the snapshot only costs one over-cautious sync.
        }
    }

    public static void Delete(string emulatorKey)
    {
        try
        {
            string path = PathFor(emulatorKey);
            if (File.Exists(path)) File.Delete(path);
        }
        catch { /* best effort */ }
    }
}
