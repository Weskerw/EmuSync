using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace EmuSync.Core;

/// <summary>Information about a remote file on Drive.</summary>
public class RemoteFile
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Md5 { get; set; }
    public DateTime? ModifiedTimeUtc { get; set; }
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

public class GoogleDriveClient : IDisposable
{
    private const string FolderMime = "application/vnd.google-apps.folder";
    private DriveService? _service;

    public bool IsConnected => _service != null;

    /// <summary>
    /// True if an OAuth token from a previous sign-in is stored:
    /// in that case ConnectAsync will not open the browser.
    /// </summary>
    public static bool HasStoredToken =>
        Directory.Exists(AppConfig.TokenDir) && Directory.EnumerateFiles(AppConfig.TokenDir).Any();

    /// <summary>
    /// Desktop OAuth authentication (Windows/Linux/macOS): the first run opens
    /// the browser for consent ("Sign in with Google"), then the stored token
    /// is reused. End users don't need to configure anything: the app
    /// credentials are embedded (BuiltInCredentials) or, if present, a
    /// credentials.json next to the executable is used instead.
    /// A dedicated flow will be needed on Android, but the rest of this class is reusable.
    /// </summary>
    public async Task ConnectAsync(string? credentialsPath = null, CancellationToken ct = default)
    {
        if (_service != null) return;

        ClientSecrets secrets;
        if (credentialsPath != null && File.Exists(credentialsPath))
        {
            using var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read);
            secrets = GoogleClientSecrets.FromStream(stream).Secrets;
        }
        else if (BuiltInCredentials.Available)
        {
            secrets = new ClientSecrets
            {
                ClientId = BuiltInCredentials.ClientId,
                ClientSecret = BuiltInCredentials.ClientSecret
            };
        }
        else
        {
            throw new InvalidOperationException(
                "No OAuth credentials available.\n" +
                "Fill in BuiltInCredentials.cs (see README) or place a " +
                "credentials.json next to the executable.");
        }

        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            secrets,
            new[] { DriveService.Scope.DriveFile },
            "user",
            ct,
            new FileDataStore(AppConfig.TokenDir, true));

        _service = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "EmuSync"
        });
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
            list.Fields = "nextPageToken, files(id, name, mimeType, md5Checksum, modifiedTime)";
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
                        ParentId = folderId
                    };
                }
            }
            pageToken = page.NextPageToken;
        } while (pageToken != null);
    }

    /// <summary>Uploads a new file, preserving the local modification time.</summary>
    public async Task UploadNewAsync(string localPath, string name, string parentId, CancellationToken ct = default)
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
    /// Signs out: disconnects and deletes the stored token, so the next
    /// ConnectAsync opens the browser and asks for an account again.
    /// </summary>
    public void SignOut()
    {
        _service?.Dispose();
        _service = null;
        if (Directory.Exists(AppConfig.TokenDir))
            Directory.Delete(AppConfig.TokenDir, true);
    }

    public void Dispose() => _service?.Dispose();
}
