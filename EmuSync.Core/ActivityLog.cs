namespace EmuSync.Core;

/// <summary>What happened to one file during a sync.</summary>
public enum SyncAction
{
    Uploaded,
    Downloaded,
    DeletedLocal,
    DeletedRemote,
    Conflict
}

/// <summary>One line of the history: a single file, and what was done with it.</summary>
public class SyncOperation
{
    public SyncAction Action { get; set; }

    /// <summary>Path relative to the emulator's save folder, '/' separated.</summary>
    public string Path { get; set; } = "";

    public long Size { get; set; }
    public string Md5 { get; set; } = "";
    public DateTime AtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Short label for the UI, in the user's reading direction.</summary>
    public string ActionLabel => Action switch
    {
        SyncAction.Uploaded => "↑ uploaded",
        SyncAction.Downloaded => "↓ downloaded",
        SyncAction.DeletedLocal => "✗ deleted here",
        SyncAction.DeletedRemote => "✗ deleted on Drive",
        SyncAction.Conflict => "⚠ conflict",
        _ => Action.ToString()
    };
}

/// <summary>
/// The record of one sync of one emulator: who ran it, when, on what, and every
/// file it touched. Stored as a single document in
/// <c>users/{uid}/activity/{runId}</c> — one write per sync, whatever the number
/// of files, which keeps the history essentially free.
/// </summary>
public class SyncRunLog
{
    /// <summary>Beyond this many operations the list is cut, to stay well under
    /// Firestore's 1 MiB document limit.</summary>
    public const int MaxOperations = 400;

    /// <summary>How long history is kept before it is pruned.</summary>
    public static readonly TimeSpan Retention = TimeSpan.FromDays(365);

    public string Id { get; set; } = "";

    public string EmulatorKey { get; set; } = "";
    public string EmulatorName { get; set; } = "";
    public string Console { get; set; } = "";

    public string DeviceId { get; set; } = "";
    public string DeviceName { get; set; } = "";

    public DateTime StartedUtc { get; set; }
    public int DurationMs { get; set; }

    public int Uploaded { get; set; }
    public int Downloaded { get; set; }
    public int Skipped { get; set; }
    public int Conflicts { get; set; }
    public int DeletedLocal { get; set; }
    public int DeletedRemote { get; set; }

    /// <summary>Total bytes transferred in both directions.</summary>
    public long Bytes { get; set; }

    /// <summary>Set when the sync failed: the history keeps failures too.</summary>
    public string? Error { get; set; }

    /// <summary>True when there were more operations than the document can hold.</summary>
    public bool Truncated { get; set; }

    public List<SyncOperation> Operations { get; set; } = new();

    /// <summary>True when the run moved nothing at all (everything already in sync).</summary>
    public bool IsNoOp => Uploaded + Downloaded + DeletedLocal + DeletedRemote + Conflicts == 0 && Error == null;

    public string Summary
    {
        get
        {
            if (Error != null) return "failed";
            if (IsNoOp) return $"nothing to do ({Skipped} files already in sync)";

            var parts = new List<string>();
            if (Uploaded > 0) parts.Add($"{Uploaded} uploaded");
            if (Downloaded > 0) parts.Add($"{Downloaded} downloaded");
            if (DeletedLocal + DeletedRemote > 0) parts.Add($"{DeletedLocal + DeletedRemote} deleted");
            if (Conflicts > 0) parts.Add($"{Conflicts} conflicts");
            return string.Join(", ", parts);
        }
    }
}
