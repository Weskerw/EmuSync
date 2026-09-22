using System.Diagnostics;
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

    /// <summary>Bytes transferred, both directions.</summary>
    public long Bytes;

    /// <summary>Every file this sync touched, in order — the history's raw material.</summary>
    public List<SyncOperation> Operations { get; } = new();

    public void Record(SyncAction action, string path, long size, string md5)
    {
        Operations.Add(new SyncOperation
        {
            Action = action,
            Path = path,
            Size = size,
            Md5 = md5
        });

        if (action is SyncAction.Uploaded or SyncAction.Downloaded) Bytes += size;
    }

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
/// called on this machine.
///
/// Three states are compared for every file:
///   • the local copy;
///   • the copy on Drive;
///   • this device's snapshot of the last successful sync (<see cref="DeviceIndexStore"/>).
/// The snapshot is what tells a deletion apart from a file that simply has not
/// been downloaded here yet. Deletions are then published as tombstones in the
/// shared Firestore index, which is how they reach the other devices.
/// </summary>
public class SyncEngine
{
    /// <summary>Tolerance for time comparison (FAT filesystems round to 2 s).</summary>
    private static readonly TimeSpan Tolerance = TimeSpan.FromSeconds(3);

    private readonly GoogleDriveClient _drive;
    private readonly CloudStore _cloud;
    private readonly string _deviceId;
    private readonly string _deviceName;

    /// <summary>Root folder on Drive, as configured by the user.</summary>
    private readonly string _rootPath;

    public SyncEngine(GoogleDriveClient drive, CloudStore cloud, string deviceId, string deviceName, string rootPath)
    {
        _drive = drive;
        _cloud = cloud;
        _deviceId = deviceId;
        _deviceName = deviceName;
        _rootPath = DrivePath.Normalize(rootPath);
    }

    /// <summary>
    /// Syncs one emulator and records the run in the shared history, successes and
    /// failures alike.
    /// </summary>
    public async Task<SyncStats> SyncProfileAsync(EmulatorProfile profile, Action<string> log, CancellationToken ct = default)
    {
        var stats = new SyncStats();
        var startedUtc = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        string? error = null;

        try
        {
            await SyncCoreAsync(profile, stats, log, ct);
            return stats;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            throw;
        }
        finally
        {
            await WriteHistoryAsync(profile, stats, startedUtc, stopwatch.ElapsedMilliseconds, error, ct);
        }
    }

    /// <summary>Writes the history entry. Never throws: history is not worth a failed sync.</summary>
    private async Task WriteHistoryAsync(EmulatorProfile profile, SyncStats stats, DateTime startedUtc,
        long durationMs, string? error, CancellationToken ct)
    {
        // Nothing moved and nothing broke: don't fill the history with "checked,
        // all fine" from every periodic poll of every device.
        if (error == null && stats.Operations.Count == 0) return;

        try
        {
            await _cloud.SaveRunAsync(new SyncRunLog
            {
                Id = $"{startedUtc:yyyyMMdd'T'HHmmssfff}-{_deviceId}-{profile.Key}",
                EmulatorKey = profile.Key,
                EmulatorName = profile.DisplayName,
                Console = profile.Console,
                DeviceId = _deviceId,
                DeviceName = _deviceName,
                StartedUtc = startedUtc,
                DurationMs = (int)Math.Min(durationMs, int.MaxValue),
                Uploaded = stats.Uploaded,
                Downloaded = stats.Downloaded,
                Skipped = stats.Skipped,
                Conflicts = stats.Conflicts,
                DeletedLocal = stats.DeletedLocal,
                DeletedRemote = stats.DeletedRemote,
                Bytes = stats.Bytes,
                Error = error,
                Operations = stats.Operations
            }, CancellationToken.None);
        }
        catch
        {
            // Offline, rules refused, quota exhausted: the sync itself still stands.
        }
    }

    private async Task SyncCoreAsync(EmulatorProfile profile, SyncStats stats, Action<string> log,
        CancellationToken ct)
    {
        if (!profile.IsLinkedHere)
            throw new InvalidOperationException($"No local folder set for '{profile.DisplayName}' on this device.");
        if (!Directory.Exists(profile.LocalPath))
            throw new DirectoryNotFoundException($"Local folder not found: {profile.LocalPath}");

        log($"— {profile.DisplayName} ({profile.Console}) —");

        // 1. Remote folder: <configured folder>/<emulator key>
        string rootId = await _drive.EnsurePathAsync(_rootPath, ct);
        string emulatorFolderId = await _drive.EnsureFolderAsync(profile.Key, rootId, ct);

        // 2. The three states.
        log("Reading the remote file list...");
        var remote = await _drive.ListRecursiveAsync(emulatorFolderId, ct);
        var index = await _cloud.LoadIndexAsync(profile.Key, ct);
        var snapshot = DeviceIndexStore.Load(profile.Key);

        // A snapshot taken against a different Drive folder says nothing about
        // this one: start over rather than read it as a pile of deletions.
        if (snapshot.Files.Count > 0 && !string.Equals(snapshot.RootPath, _rootPath, StringComparison.OrdinalIgnoreCase))
        {
            log($"The Drive folder changed ({snapshot.RootPath} → {_rootPath}): starting from a clean state.");
            snapshot = new DeviceSnapshot { EmulatorKey = profile.Key };
        }

        var local = Directory.EnumerateFiles(profile.LocalPath, "*", SearchOption.AllDirectories)
            .Where(p => !p.EndsWith(".emusync-tmp", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(
                p => Path.GetRelativePath(profile.LocalPath, p).Replace('\\', '/'),
                p => p,
                StringComparer.OrdinalIgnoreCase);

        // Safety nets. An empty side almost always means "not ready" (emulator not
        // installed here, external drive not mounted, Drive folder recreated), not
        // "the user deleted everything" — and a wrong guess here costs real saves.
        bool allowRemoteDeletions = local.Count > 0;
        bool allowLocalDeletions = remote.Files.Count > 0;

        if (!allowRemoteDeletions)
            log("⚠ The local folder is empty: deletions will not be propagated to Drive.");
        if (!allowLocalDeletions)
            log("⚠ The remote folder is empty: nothing will be deleted locally.");

        var newSnapshot = new DeviceSnapshot { EmulatorKey = profile.Key, RootPath = _rootPath };

        var allPaths = local.Keys
            .Union(remote.Files.Keys, StringComparer.OrdinalIgnoreCase)
            .Union(snapshot.Files.Keys, StringComparer.OrdinalIgnoreCase)
            .Union(index.Deleted.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);

        foreach (string relPath in allPaths)
        {
            ct.ThrowIfCancellationRequested();

            bool hasLocal = local.TryGetValue(relPath, out string? localPath);
            bool hasRemote = remote.Files.TryGetValue(relPath, out RemoteFile? remoteFile);
            snapshot.Files.TryGetValue(relPath, out SnapshotEntry? known);
            index.Deleted.TryGetValue(relPath, out Tombstone? tombstone);

            // ---------------------------------------------------- both present
            if (hasLocal && hasRemote)
            {
                string localMd5 = ComputeMd5(localPath!);
                string remoteMd5 = remoteFile!.Md5 ?? "";

                // The file is back on both sides: any old tombstone is void.
                index.Deleted.Remove(relPath);

                if (!string.IsNullOrEmpty(remoteMd5) && Same(localMd5, remoteMd5))
                {
                    stats.Skipped++;
                    newSnapshot.Files[relPath] = Snap(localMd5, localPath!);
                    continue;
                }

                bool localChanged = known == null || !Same(localMd5, known.Md5);
                bool remoteChanged = known == null || !Same(remoteMd5, known.Md5);

                DateTime localTime = File.GetLastWriteTimeUtc(localPath!);
                DateTime remoteTime = remoteFile.ModifiedTimeUtc ?? DateTime.MinValue;

                SyncAction action;

                // Only one side moved since the last sync: no ambiguity at all.
                if (localChanged && !remoteChanged)
                {
                    log($"↑ Updating on Drive: {relPath}");
                    await _drive.UpdateAsync(remoteFile.Id, localPath!, ct);
                    stats.Uploaded++;
                    action = SyncAction.Uploaded;
                }
                else if (remoteChanged && !localChanged)
                {
                    log($"↓ Downloading: {relPath}");
                    await _drive.DownloadAsync(remoteFile, localPath!, ct);
                    stats.Downloaded++;
                    localMd5 = remoteMd5;
                    action = SyncAction.Downloaded;
                }
                else if (localTime - remoteTime > Tolerance)
                {
                    log($"↑ Both changed, the local file is newer: {relPath}");
                    await _drive.UpdateAsync(remoteFile.Id, localPath!, ct);
                    stats.Uploaded++;
                    action = SyncAction.Uploaded;
                }
                else if (remoteTime - localTime > Tolerance)
                {
                    log($"↓ Both changed, the remote file is newer: {relPath}");
                    await _drive.DownloadAsync(remoteFile, localPath!, ct);
                    stats.Downloaded++;
                    localMd5 = remoteMd5;
                    action = SyncAction.Downloaded;
                }
                else
                {
                    // Same timestamp, different content: guessing could throw away
                    // hours of play, so leave both copies where they are.
                    log($"⚠ Conflict (same time, different content), skipped: {relPath}");
                    stats.Conflicts++;
                    stats.Record(SyncAction.Conflict, relPath, SafeLength(localPath!), localMd5);
                    if (known != null) newSnapshot.Files[relPath] = known;
                    continue;
                }

                var synced = Snap(localMd5, localPath!);
                stats.Record(action, relPath, synced.Size, synced.Md5);
                newSnapshot.Files[relPath] = synced;
                continue;
            }

            // ------------------------------------------- only on this computer
            if (hasLocal)
            {
                string localMd5 = ComputeMd5(localPath!);

                // Deleted on another device, and untouched here since: follow suit.
                if (tombstone != null && Same(localMd5, tombstone.Md5))
                {
                    // ...unless the safety net is up. It must block the deletion
                    // without cancelling it: falling through would re-upload the
                    // file and drop the tombstone, undoing the deletion for
                    // everyone just because one listing came back empty.
                    if (!allowLocalDeletions)
                    {
                        newSnapshot.Files[relPath] = Snap(localMd5, localPath!);
                        continue;
                    }

                    log($"✗ Deleted on another device, removing locally: {relPath}");
                    long deletedSize = SafeLength(localPath!);
                    MoveToLocalTrash(profile.Key, relPath, localPath!);
                    stats.DeletedLocal++;
                    stats.Record(SyncAction.DeletedLocal, relPath, deletedSize, localMd5);
                    continue;
                }

                // Either brand new, or changed here after someone deleted it
                // elsewhere — in which case the newer work wins and comes back.
                if (tombstone != null)
                {
                    log($"↑ Changed here after being deleted elsewhere, restoring: {relPath}");
                    index.Deleted.Remove(relPath);
                }
                else
                {
                    log(known == null ? $"↑ Uploading new file: {relPath}" : $"↑ Re-uploading: {relPath}");
                }

                string parentId = await EnsureRemoteDirAsync(remote, emulatorFolderId, GetDir(relPath), ct);
                await _drive.UploadNewAsync(localPath!, Path.GetFileName(localPath!), parentId, ct);
                stats.Uploaded++;

                var uploaded = Snap(localMd5, localPath!);
                stats.Record(SyncAction.Uploaded, relPath, uploaded.Size, uploaded.Md5);
                newSnapshot.Files[relPath] = uploaded;
                continue;
            }

            // --------------------------------------------------- only on Drive
            if (hasRemote)
            {
                // In THIS device's snapshot and unchanged on Drive: it was deleted
                // here. Without the per-device snapshot this is indistinguishable
                // from "another device just uploaded it", which is why the shared
                // index must never be used for this decision.
                if (known != null && Same(remoteFile!.Md5 ?? "", known.Md5) && allowRemoteDeletions)
                {
                    log($"✗ Deleted locally, moving to the Drive trash: {relPath}");
                    await _drive.TrashAsync(remoteFile!.Id, ct);
                    index.Deleted[relPath] = new Tombstone
                    {
                        Path = relPath,
                        Md5 = known.Md5,
                        DeletedUtc = DateTime.UtcNow,
                        DeviceId = _deviceId
                    };
                    stats.DeletedRemote++;
                    stats.Record(SyncAction.DeletedRemote, relPath, remoteFile!.Size, known.Md5);
                    continue;
                }

                log($"↓ Downloading: {relPath}");
                string dest = Path.Combine(profile.LocalPath, relPath.Replace('/', Path.DirectorySeparatorChar));
                await _drive.DownloadAsync(remoteFile!, dest, ct);
                stats.Downloaded++;
                index.Deleted.Remove(relPath);

                var downloaded = Snap(remoteFile!.Md5 ?? ComputeMd5(dest), dest);
                stats.Record(SyncAction.Downloaded, relPath, downloaded.Size, downloaded.Md5);
                newSnapshot.Files[relPath] = downloaded;
                continue;
            }

            // Gone from both sides: drop it from the snapshot (the loop simply
            // does not carry it over) and keep any tombstone until it expires.
        }

        // 3. Persist the new states: the snapshot locally, the summary and the
        //    tombstones in the shared index.
        DeviceIndexStore.Save(newSnapshot);

        index.EmulatorKey = profile.Key;
        index.TotalBytes = newSnapshot.Files.Values.Sum(e => e.Size);
        index.Files = newSnapshot.Files
            // A file kept alive by a blocked deletion is still tombstoned: listing
            // it as synced would contradict the tombstone on the other devices.
            .Where(kv => !index.Deleted.ContainsKey(kv.Key))
            .Select(kv => new IndexEntry
            {
                Path = kv.Key,
                Md5 = kv.Value.Md5,
                Size = kv.Value.Size,
                ModifiedUtc = kv.Value.ModifiedUtc
            })
            .ToList();
        index.FileCount = index.Files.Count; // set before the list is capped for storage

        await _cloud.SaveIndexAsync(index, _deviceId, ct);
        profile.LastSyncUtc = index.LastSyncUtc;

        log($"{profile.DisplayName}: {stats}");
    }

    private static bool Same(string a, string b) =>
        !string.IsNullOrEmpty(a) && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static SnapshotEntry Snap(string md5, string localPath) => new()
    {
        Md5 = md5,
        Size = SafeLength(localPath),
        ModifiedUtc = SafeWriteTime(localPath)
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
