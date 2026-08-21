namespace EmuSync.Core;

/// <summary>
/// A sync profile: a local folder (e.g. PCSX2 memory cards) mapped to a
/// remote folder on Google Drive (EmuSync/&lt;Name&gt;).
/// </summary>
public class SyncProfile
{
    public string Name { get; set; } = "";
    public string LocalPath { get; set; } = "";

    /// <summary>Date/time (UTC) of the last successful synchronization.</summary>
    public DateTime? LastSyncUtc { get; set; }
}
