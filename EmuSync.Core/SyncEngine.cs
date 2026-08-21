using System.Security.Cryptography;

namespace EmuSync.Core;

public class SyncStats
{
    public int Uploaded;
    public int Downloaded;
    public int Skipped;
    public int Conflicts;

    public override string ToString() =>
        $"{Uploaded} uploaded, {Downloaded} downloaded, {Skipped} unchanged, {Conflicts} conflicts";
}

/// <summary>
/// Two-way sync engine: compares a profile's local folder with Drive
/// (EmuSync/&lt;ProfileName&gt;) and always propagates the most recent file.
/// Comparison: MD5 hash first (if equal, do nothing), then last modification time.
/// </summary>
public class SyncEngine
{
    /// <summary>Tolerance for time comparison (FAT filesystems round to 2 s).</summary>
    private static readonly TimeSpan Tolerance = TimeSpan.FromSeconds(3);

    private const string RootFolderName = "EmuSync";

    private readonly GoogleDriveClient _drive;

    public SyncEngine(GoogleDriveClient drive) => _drive = drive;

    public async Task<SyncStats> SyncProfileAsync(SyncProfile profile, Action<string> log, CancellationToken ct = default)
    {
        var stats = new SyncStats();

        if (!Directory.Exists(profile.LocalPath))
            throw new DirectoryNotFoundException($"Local folder not found: {profile.LocalPath}");

        log($"— Profile '{profile.Name}' —");
        log("Looking for the remote folder on Drive...");
        string rootId = await _drive.EnsureFolderAsync(RootFolderName, null, ct);
        string profileFolderId = await _drive.EnsureFolderAsync(profile.Name, rootId, ct);

        log("Reading the remote file list...");
        var remote = await _drive.ListRecursiveAsync(profileFolderId, ct);

        // Local files: relative path with '/' separator
        var local = Directory.EnumerateFiles(profile.LocalPath, "*", SearchOption.AllDirectories)
            .ToDictionary(
                p => Path.GetRelativePath(profile.LocalPath, p).Replace('\\', '/'),
                p => p,
                StringComparer.OrdinalIgnoreCase);

        var allPaths = local.Keys.Union(remote.Files.Keys, StringComparer.OrdinalIgnoreCase)
                                 .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);

        foreach (string relPath in allPaths)
        {
            ct.ThrowIfCancellationRequested();
            bool hasLocal = local.TryGetValue(relPath, out string? localPath);
            bool hasRemote = remote.Files.TryGetValue(relPath, out RemoteFile? remoteFile);

            if (hasLocal && !hasRemote)
            {
                log($"↑ Uploading new file: {relPath}");
                string parentId = await EnsureRemoteDirAsync(remote, profileFolderId, GetDir(relPath), ct);
                await _drive.UploadNewAsync(localPath!, Path.GetFileName(localPath!), parentId, ct);
                stats.Uploaded++;
            }
            else if (!hasLocal && hasRemote)
            {
                log($"↓ Downloading new file: {relPath}");
                string dest = Path.Combine(profile.LocalPath, relPath.Replace('/', Path.DirectorySeparatorChar));
                await _drive.DownloadAsync(remoteFile!, dest, ct);
                stats.Downloaded++;
            }
            else if (hasLocal && hasRemote)
            {
                // Identical content? (Drive exposes the MD5 of binary files)
                if (!string.IsNullOrEmpty(remoteFile!.Md5) &&
                    string.Equals(remoteFile.Md5, ComputeMd5(localPath!), StringComparison.OrdinalIgnoreCase))
                {
                    stats.Skipped++;
                    continue;
                }

                DateTime localTime = File.GetLastWriteTimeUtc(localPath!);
                DateTime remoteTime = remoteFile.ModifiedTimeUtc ?? DateTime.MinValue;

                if (localTime - remoteTime > Tolerance)
                {
                    log($"↑ Local file is newer, updating on Drive: {relPath}");
                    await _drive.UpdateAsync(remoteFile.Id, localPath!, ct);
                    stats.Uploaded++;
                }
                else if (remoteTime - localTime > Tolerance)
                {
                    log($"↓ Remote file is newer, downloading: {relPath}");
                    await _drive.DownloadAsync(remoteFile, localPath!, ct);
                    stats.Downloaded++;
                }
                else
                {
                    // Same time but different content: cannot decide automatically.
                    log($"⚠ Conflict (same time, different content), skipping: {relPath}");
                    stats.Conflicts++;
                }
            }
        }

        log($"Profile '{profile.Name}' done: {stats}");
        return stats;
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
