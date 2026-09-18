namespace EmuSync.Core;

/// <summary>
/// One emulator being synced on this device: the catalog entry, where its saves
/// live locally (device-specific) and when it was last synced (shared).
///
/// The remote folder is always <c>EmuSync/&lt;Key&gt;</c>, so the same emulator
/// maps to the same folder on every device even when the local paths differ.
/// </summary>
public class EmulatorProfile
{
    public EmulatorProfile() { }

    public EmulatorProfile(EmulatorInfo emulator, string localPath)
    {
        Key = emulator.Key;
        DisplayName = emulator.DisplayName;
        Console = emulator.Console;
        LocalPath = localPath;
    }

    /// <summary>Emulator key: also the folder name on Drive and the Firestore document id.</summary>
    public string Key { get; set; } = "";

    /// <summary>Human-readable name, kept in the cloud config so other devices show the same label.</summary>
    public string DisplayName { get; set; } = "";

    /// <summary>The emulated console, for display only.</summary>
    public string Console { get; set; } = "";

    /// <summary>Local save folder on THIS device (stored per-device, not shared).</summary>
    public string LocalPath { get; set; } = "";

    /// <summary>When false the emulator stays in the config but is skipped.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Date/time (UTC) of the last successful sync, shared across devices.</summary>
    public DateTime? LastSyncUtc { get; set; }

    /// <summary>False when this device has no local folder set for the emulator yet.</summary>
    public bool IsLinkedHere => !string.IsNullOrWhiteSpace(LocalPath);

    public EmulatorInfo Info => EmulatorCatalog.Resolve(Key);
}
