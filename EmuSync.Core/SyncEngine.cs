using System.Security.Cryptography;

namespace EmuSync.Core;

public class SyncStats
{
    public int Uploaded;
    public int Downloaded;
    public int Skipped;
    public int Conflicts;
    public int DeletedLocal;
    public int DeletedRemote;

    public override string ToString()
    {
        string text = $"{Uploaded} uploaded, {Downloaded} downloaded, {Skipped} unchanged";
        if (DeletedLocal + DeletedRemote > 0) text += $", {DeletedLocal + DeletedRemote} deleted";
        if (Conflicts > 0) text += $", {Conflicts} conflicts";
        return text;
    }
}

/// <summary>
/// Two-way sync engine, one emulator at a time.
///
/// Layout: the local save folder of an emulator maps to <c>EmuSync/&lt;key&gt;</c>
/// on Drive — PCSX2 always lands in <c>EmuSync/pcsx2</c>, whatever the folder is
/// called on this machine — while Firestore keeps the index of what was last
/// synced.
///
/// That index is what turns "copy the newest file" into a real sync: comparing
/// local, remote and last-known state tells a deletion apart from a file that was
/// simply never downloaded, which the old folder-to-folder sync could not do.
/// </summary>
public class SyncEngine
{
    /// <summary>Tolerance for time comparison (FAT filesystems round to 2 s).</summary>
    private static readonly TimeSpan Tolerance = TimeSpan.FromSeconds(3);

    private const string RootFolderName = "EmuSync";

    private readonly GoogleDriveClient _drive;
    private readonly CloudStore _cloud;
    private readonly string _deviceId;

    public SyncEngine(GoogleDriveClient drive, CloudStore cloud, string deviceId)
    {
        _drive = drive;
        _cloud = cloud;
        _deviceId = deviceId;
    }

    public async Task<SyncStats> SyncProfileAsync(EmulatorProfile profile, Action<string> log, CancellationToken ct = default)
    {
        var stats = new SyncStats();

        if (!profile.IsLinkedHere)
            throw new InvalidOperationException($"No local folder set for '{profile.DisplayName}' on this device.");
        if (!Directory.Exists(profile.LocalPath))
            throw new DirectoryNotFoundException($"Local folder not found: {profile.LocalPath}");

        log($"— {profile.DisplayName} ({profile.Console}) —");

        // 1. Remote folder: EmuSync/<emulator key>
        string rootId = await _drive.EnsureFolderAsync(RootFolderName, null, ct);
        string emulatorFolderId = await _drive.EnsureFolderAsync(profile.Key, rootId, ct);

        // 2. The three states to compare.
        log("Reading the remote file list...");
        var remote = await _drive.ListRecursiveAsync(emulatorFolderId, ct);
        var index = await _cloud.LoadIndexAsync(profile.Key, ct);

        var local = Directory.EnumerateFiles(profile.LocalPath, "*", SearchOption.AllDirectories)
            .Where(p => !p.EndsWith(".emusync-tmp", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(
                p => Path.GetRelativePath(profile.LocalPath, p).Replace('\\', '/'),
                p => p,
                StringComparer.OrdinalIgnoreCase);

        // Safety net: an empty folder usually means the emulator is not installed
        // here (or the drive is not mounted yet), not that the user deleted every
        // save. Never let that wipe the cloud copy.
        bool allowDeletions = local.Count > 0 || index.Entries.Count == 0;
        if (!allowDeletions)
            log("⚠ The local folder is empty: deletions will not be propagated this time.");

        var newIndex = new RemoteIndex { EmulatorKey = profile.Key };

        var allPaths = local.Keys
            .Union(remote.Files.Keys, StringComparer.OrdinalIgnoreCase)
            .Union(index.Entries.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);

        foreach (string relPath in allPaths)
        {
            ct.ThrowIfCancellationRequested();

            bool hasLocal = local.TryGetValue(relPath, out string? localPath);
            bool hasRemote = remote.Files.TryGetValue(relPath, out RemoteFile? remoteFile);
            index.Entries.TryGetValue(relPath, out IndexEntry? known);

            // ---------------------------------------------------- both present
            if (hasLocal && hasRemote)
            {
                string localMd5 = ComputeMd5(localPath!);
                string remoteMd5 = remoteFile!.Md5 ?? "";

                if (!string.IsNullOrEmpty(remoteMd5) &&
                    string.Equals(localMd5, remoteMd5, StringComparison.OrdinalIgnoreCase))
                {
                    stats.Skipped++;
                    newIndex.Entries[relPath] = Entry(relPath, localMd5, localPath!, remoteFile.Id);
                    continue;
                }

                bool localChanged = known == null || !string.Equals(localMd5, known.Md5, StringComparison.OrdinalIgnoreCase);
                bool remoteChanged = known == null || !string.Equals(remoteMd5, known.Md5, StringComparison.OrdinalIgnoreCase);

                DateTime localTime = File.GetLastWriteTimeUtc(localPath!);
                DateTime remoteTime = remoteFile.ModifiedTimeUtc ?? DateTime.MinValue;

                // Only one side moved since the last sync: no ambiguity.
                if (localChanged && !remoteChanged)
                {
                    log($"↑ Updating on Drive: {relPath}");
                    await _drive.UpdateAsync(remoteFile.Id, localPath!, ct);
                    stats.Uploaded++;
                }
                else if (remoteChanged && !localChanged)
                {
                    log($"↓ Downloading: {relPath}");
                    await _drive.DownloadAsync(remoteFile, localPath!, ct);
                    stats.Downloaded++;
                    localMd5 = remoteMd5;
                }
                else if (localTime - remoteTime > Tolerance)
                {
                    log($"↑ Both changed, the local file is newer: {relPath}");
                    await _drive.UpdateAsync(remoteFile.Id, localPath!, ct);
                    stats.Uploaded++;
                }
                else if (remoteTime - localTime > Tolerance)
                {
                    log($"↓ Both changed, the remote file is newer: {relPath}");
                    await _drive.DownloadAsync(remoteFile, localPath!, ct);
                    stats.Downloaded++;
                    localMd5 = remoteMd5;
                }
                else
                {
                    // Same timestamp, different content: guessing here could throw
                    // away hours of play, so leave both copies alone.
                    log($"⚠ Conflict (same time, different content), skipped: {relPath}");
                    stats.Conflicts++;
                    if (known != null) newIndex.Entries[relPath] = known;
                    continue;
                }

                newIndex.Entries[relPath] = Entry(relPath, localMd5, localPath!, remoteFile.Id);
                continue;
            }

            // ------------------------------------------- only on this computer
            if (hasLocal)
            {
                string localMd5 = ComputeMd5(localPath!);

                // Known to the index and unchanged here: it was deleted elsewhere.
                if (known != null && string.Equals(localMd5, known.Md5, StringComparison.OrdinalIgnoreCase))
                {
                    log($"✗ Deleted on another device, removing locally: {relPath}");
                    MoveToLocalTrash(profile.Key, relPath, localPath!);
                    stats.DeletedLocal++;
                    continue;
                }

                log(known == null ? $"↑ Uploading new file: {relPath}" : $"↑ Re-uploading modified file: {relPath}");
                string parentId = await EnsureRemoteDirAsync(remote, emulatorFolderId, GetDir(relPath), ct);
                string driveId = await _drive.UploadNewAsync(localPath!, Path.GetFileName(localPath!), parentId, ct);
                stats.Uploaded++;
                newIndex.Entries[relPath] = Entry(relPath, localMd5, localPath!, driveId);
                continue;
            }

            // ------------------------------------------------- only on Drive
            if (hasRemote)
            {
                bool remoteUnchanged = known != null &&
                    string.Equals(remoteFile!.Md5 ?? "", known.Md5, StringComparison.OrdinalIgnoreCase);

                // Known, unchanged remotely, gone locally: the user deleted it here.
                if (remoteUnchanged && allowDeletions)
                {
                    log($"✗ Deleted locally, moving to the Drive trash: {relPath}");
                    await _drive.TrashAsync(remoteFile!.Id, ct);
                    stats.DeletedRemote++;
                    continue;
                }

                log($"↓ Downloading: {relPath}");
                string dest = Path.Combine(profile.LocalPath, relPath.Replace('/', Path.DirectorySeparatorChar));
                await _drive.DownloadAsync(remoteFile!, dest, ct);
                stats.Downloaded++;
                newIndex.Entries[relPath] = Entry(relPath, remoteFile!.Md5 ?? ComputeMd5(dest), dest, remoteFile.Id);
                continue;
            }

            // Gone from both sides: just drop the stale index entry.
        }

        // 3. Publish the new index for the other devices.
        await _cloud.SaveIndexAsync(newIndex, _deviceId, ct);
        profile.LastSyncUtc = newIndex.LastSyncUtc;

        log($"{profile.DisplayName}: {stats}");
        return stats;
    }

    private static IndexEntry Entry(string relPath, string md5, string localPath, string driveId) => new()
    {
        Path = relPath,
        Md5 = md5,
        Size = SafeLength(localPath),
        ModifiedUtc = SafeWriteTime(localPath),
        DriveId = driveId
    };

    private static long SafeLength(string path)
    {
        try { return new FileInfo(path).Length; } catch { return 0; }
    }

    private static DateTime SafeWriteTime(string path)
    {
        try { return File.GetLastWriteTimeUtc(path); } catch { return DateTime.UtcNow; }
    }

    /// <summary>
    /// Deleting a save is never really undoable, so a file removed because another
    /// device deleted it is parked under %APPDATA%\EmuSync\trash instead.
    /// </summary>
    private static void MoveToLocalTrash(string emulatorKey, string relPath, string localPath)
    {
        try
        {
            string trashDir = Path.Combine(AppConfig.ConfigDir, "trash", emulatorKey,
                DateTime.Now.ToString("yyyyMMdd"));
            string dest = Path.Combine(trashDir, relPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            if (File.Exists(dest)) File.Delete(dest);
            File.Move(localPath, dest);
        }
        catch
        {
            // Could not park it: delete it anyway, the Drive trash still has a copy.
            try { File.Delete(localPath); } catch { /* give up quietly */ }
        }
    }

    private static string GetDir(string relPath)
    {
        int i = relPath.LastIndexOf('/');
        return i < 0 ? "" : relPath[..i];
    }

    /// <summary>Finds or creates (recursively) the remote subfolder matching relDir.</summary>
    private async Task<string> EnsureRemoteDirAsync(RemoteListing remote, string rootId, string relDir, CancellationToken ct)
    {
        if (relDir.Length == 0) return rootId;
        if (remote.Folders.TryGetValue(relDir, out string? cached)) return cached;

        string parentId = await EnsureRemoteDirAsync(remote, rootId, GetDir(relDir), ct);
        string name = relDir[(relDir.LastIndexOf('/') + 1)..];
        string id = await _drive.EnsureFolderAsync(name, parentId, ct);
        remote.Folders[relDir] = id;
        return id;
    }

    private static string ComputeMd5(string path)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(md5.ComputeHash(stream)).ToLowerInvariant();
    }
}
