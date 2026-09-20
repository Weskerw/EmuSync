using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace EmuSync.Core;

/// <summary>Information about a remote file on Drive.</summary>
public class RemoteFile
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Md5 { get; set; }
    public DateTime? ModifiedTimeUtc { get; set; }
    public long Size { get; set; }
    public string ParentId { get; set; } = "";
}

/// <summary>Result of the recursive listing of a remote folder.</summary>
public class RemoteListing
{
    /// <summary>relative path ('/' separator) -> file</summary>
    public Dictionary<string, RemoteFile> Files { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>relative folder path ("" = root) -> Drive folder id</summary>
    public Dictionary<string, string> Folders { get; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Drive access for the save files themselves. The account is the user's own, so
/// the bytes never leave their Drive quota; Firebase only holds the configuration
/// and the file index.
///
/// The consent is obtained through <see cref="IGoogleAuthorizationProvider"/>,
/// which is the only platform-specific dependency.
/// </summary>
public class GoogleDriveClient : IDisposable
{
    private const string FolderMime = "application/vnd.google-apps.folder";

    private readonly IGoogleAuthorizationProvider _authProvider;
    private DriveService? _service;

    public GoogleDriveClient(IGoogleAuthorizationProvider authProvider) => _authProvider = authProvider;

    public bool IsConnected => _service != null;

    /// <summary>True when a previous sign-in left a token: connecting will not open the browser.</summary>
    public bool HasStoredToken => _authProvider.HasStoredToken;

    /// <summary>
    /// The Google ID token from the last authorization, if any. Used to sign the
    /// user in to Firebase with the same consent.
    /// </summary>
    public string? LastIdToken { get; private set; }

    /// <summary>
    /// Connects to Drive, reusing the stored token when possible. If Google
    /// rejects it (revoked or expired) the consent flow runs once more.
    /// </summary>
    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (_service != null) return;

        try
        {
            await AuthorizeAsync(force: false, ct);
        }
        catch (Exception ex) when (IsInvalidGrant(ex))
        {
            await AuthorizeAsync(force: true, ct);
        }
    }

    /// <summary>Forces a fresh consent (used by "change account" and after an invalid_grant).</summary>
    public Task ReauthorizeAsync(CancellationToken ct = default) => AuthorizeAsync(force: true, ct);

    /// <summary>
    /// Re-runs the (silent) authorization to obtain a fresh Google ID token.
    /// Google ID tokens are only valid for an hour, so the one captured when the
    /// app connected is usually stale by the time it would be exchanged for a
    /// Firebase session; this reuses the stored refresh token, no browser needed.
    /// </summary>
    public async Task<string?> RefreshIdTokenAsync(CancellationToken ct = default)
    {
        await AuthorizeAsync(force: false, ct);
        return LastIdToken;
    }

    private async Task AuthorizeAsync(bool force, CancellationToken ct)
    {
        _service?.Dispose();
        _service = null;

        var auth = force
            ? await _authProvider.ReauthorizeAsync(ct)
            : await _authProvider.AuthorizeAsync(ct);

        LastIdToken = auth.IdToken;

        _service = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = auth.Credential,
            ApplicationName = "EmuSync"
        });
    }

    /// <summary>
    /// True if the exception means the stored refresh token is no longer valid
    /// (revoked, expired, or the consent was withdrawn). The only cure is a new sign-in.
    /// </summary>
    public static bool IsInvalidGrant(Exception? ex)
    {
        while (ex != null)
        {
            if (ex is TokenResponseException tre &&
                string.Equals(tre.Error?.Error, "invalid_grant", StringComparison.OrdinalIgnoreCase))
                return true;

            if (ex is AggregateException agg &&
                agg.InnerExceptions.Any(inner => IsInvalidGrant(inner)))
                return true;

            ex = ex.InnerException;
        }
        return false;
    }

    private DriveService Service => _service ?? throw new InvalidOperationException("Not connected to Google Drive.");

    private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("'", "\\'");

    /// <summary>Finds or creates a folder with that name inside parentId (null = Drive root).</summary>
    public async Task<string> EnsureFolderAsync(string name, string? parentId, CancellationToken ct = default)
    {
        string parentClause = parentId == null ? "'root' in parents" : $"'{parentId}' in parents";
        var list = Service.Files.List();
        list.Q = $"name = '{Escape(name)}' and mimeType = '{FolderMime}' and {parentClause} and trashed = false";
        list.Fields = "files(id, name)";
        list.PageSize = 10;
        var result = await list.ExecuteAsync(ct);
        if (result.Files != null && result.Files.Count > 0)
            return result.Files[0].Id;

        var meta = new DriveFile { Name = name, MimeType = FolderMime };
        if (parentId != null) meta.Parents = new[] { parentId };
        var create = Service.Files.Create(meta);
        create.Fields = "id";
        var created = await create.ExecuteAsync(ct);
        return created.Id;
    }

    /// <summary>Recursively lists files and subfolders of folderId.</summary>
    public async Task<RemoteListing> ListRecursiveAsync(string folderId, CancellationToken ct = default)
    {
        var listing = new RemoteListing();
        listing.Folders[""] = folderId;
        await ListRecursiveInnerAsync(folderId, "", listing, ct);
        return listing;
    }

    private async Task ListRecursiveInnerAsync(string folderId, string relPath, RemoteListing listing, CancellationToken ct)
    {
        string? pageToken = null;
        do
        {
            var list = Service.Files.List();
            list.Q = $"'{folderId}' in parents and trashed = false";
            list.Fields = "nextPageToken, files(id, name, mimeType, md5Checksum, modifiedTime, size)";
            list.PageSize = 1000;
            list.PageToken = pageToken;
            var page = await list.ExecuteAsync(ct);

            foreach (var f in page.Files ?? Enumerable.Empty<DriveFile>())
            {
                string childPath = relPath.Length == 0 ? f.Name : relPath + "/" + f.Name;
                if (f.MimeType == FolderMime)
                {
                    listing.Folders[childPath] = f.Id;
                    await ListRecursiveInnerAsync(f.Id, childPath, listing, ct);
                }
                else
                {
                    listing.Files[childPath] = new RemoteFile
                    {
                        Id = f.Id,
                        Name = f.Name,
                        Md5 = f.Md5Checksum,
                        ModifiedTimeUtc = f.ModifiedTimeDateTimeOffset?.UtcDateTime,
                        Size = f.Size ?? 0,
                        ParentId = folderId
                    };
                }
            }
            pageToken = page.NextPageToken;
        } while (pageToken != null);
    }

    /// <summary>Uploads a new file, preserving the local modification time. Returns the Drive id.</summary>
    public async Task<string> UploadNewAsync(string localPath, string name, string parentId, CancellationToken ct = default)
    {
        var meta = new DriveFile
        {
            Name = name,
            Parents = new[] { parentId },
            ModifiedTimeDateTimeOffset = File.GetLastWriteTimeUtc(localPath)
        };
        using var stream = new FileStream(localPath, FileMode.Open, FileAccess.Read);
        var request = Service.Files.Create(meta, stream, "application/octet-stream");
        request.Fields = "id";
        var progress = await request.UploadAsync(ct);
        if (progress.Status != UploadStatus.Completed)
            throw new IOException($"Upload failed for '{name}': {progress.Exception?.Message}");
        return request.ResponseBody?.Id ?? "";
    }

    /// <summary>Updates the content of an existing file, preserving the local modification time.</summary>
    public async Task UpdateAsync(string fileId, string localPath, CancellationToken ct = default)
    {
        var meta = new DriveFile
        {
            ModifiedTimeDateTimeOffset = File.GetLastWriteTimeUtc(localPath)
        };
        using var stream = new FileStream(localPath, FileMode.Open, FileAccess.Read);
        var request = Service.Files.Update(meta, fileId, stream, "application/octet-stream");
        request.Fields = "id";
        var progress = await request.UploadAsync(ct);
        if (progress.Status != UploadStatus.Completed)
            throw new IOException($"Update failed for '{localPath}': {progress.Exception?.Message}");
    }

    /// <summary>Downloads a file and sets the local modification time to match the remote one.</summary>
    public async Task DownloadAsync(RemoteFile remote, string localPath, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
        string tmp = localPath + ".emusync-tmp";
        try
        {
            using (var stream = new FileStream(tmp, FileMode.Create, FileAccess.Write))
            {
                await Service.Files.Get(remote.Id).DownloadAsync(stream, ct);
            }
            if (File.Exists(localPath)) File.Delete(localPath);
            File.Move(tmp, localPath);
            if (remote.ModifiedTimeUtc.HasValue)
                File.SetLastWriteTimeUtc(localPath, remote.ModifiedTimeUtc.Value);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }

    /// <summary>
    /// Moves a file to the Drive trash. Used to propagate a local deletion:
    /// trashing (rather than deleting for good) keeps a 30-day safety net.
    /// </summary>
    public async Task TrashAsync(string fileId, CancellationToken ct = default)
    {
        var request = Service.Files.Update(new DriveFile { Trashed = true }, fileId);
        request.Fields = "id";
        await request.ExecuteAsync(ct);
    }

    /// <summary>Signs out: the next ConnectAsync opens the browser and asks for an account again.</summary>
    public void SignOut()
    {
        _service?.Dispose();
        _service = null;
        LastIdToken = null;
        _authProvider.SignOut();
    }

    public void Dispose() => _service?.Dispose();
}
