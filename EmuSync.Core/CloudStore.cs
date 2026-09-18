namespace EmuSync.Core;

/// <summary>User settings shared by every device, stored in <c>users/{uid}</c>.</summary>
public class CloudConfig
{
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";

    /// <summary>Sync automatically when local saves change.</summary>
    public bool AutoSync { get; set; } = true;

    /// <summary>How often (minutes) to poll the cloud for changes made elsewhere (0 = never).</summary>
    public int RemoteCheckMinutes { get; set; } = 15;

    /// <summary>The emulators the user wants to sync, in display order.</summary>
    public List<CloudEmulator> Emulators { get; set; } = new();

    public CloudEmulator? Find(string key) =>
        Emulators.FirstOrDefault(e => string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase));
}

/// <summary>An emulator enrolled for sync, as seen by every device.</summary>
public class CloudEmulator
{
    public string Key { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Console { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public DateTime AddedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>A device the user has signed in from.</summary>
public class CloudDevice
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Os { get; set; } = "";
    public DateTime? LastSeenUtc { get; set; }

    /// <summary>emulator key -> local folder on that device.</summary>
    public Dictionary<string, string> Paths { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>One file known to the cloud index.</summary>
public class IndexEntry
{
    /// <summary>Path relative to the emulator's save folder, '/' separated.</summary>
    public string Path { get; set; } = "";

    public string Md5 { get; set; } = "";
    public long Size { get; set; }
    public DateTime ModifiedUtc { get; set; }

    /// <summary>Drive file id, so a rename on Drive does not cause a re-upload.</summary>
    public string DriveId { get; set; } = "";
}

/// <summary>
/// The list of files EmuSync believes are in sync for one emulator, stored in
/// <c>users/{uid}/emulators/{key}</c>. It is what makes deletions propagate: a
/// file that is in the index but missing locally was deleted, not simply never
/// downloaded.
/// </summary>
public class RemoteIndex
{
    public string EmulatorKey { get; set; } = "";
    public DateTime? LastSyncUtc { get; set; }
    public string LastDeviceId { get; set; } = "";
    public Dictionary<string, IndexEntry> Entries { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Reads and writes the user's data in Firestore: settings, devices and the
/// per-emulator file index. Layout:
///
/// <code>
/// users/{uid}                       settings + enrolled emulators
/// users/{uid}/devices/{deviceId}    local paths of that machine
/// users/{uid}/emulators/{key}       file index of that emulator
/// </code>
///
/// Everything is scoped under <c>users/{uid}</c> so a single security rule
/// (<c>request.auth.uid == uid</c>) covers the whole tree.
/// </summary>
public class CloudStore
{
    private readonly FirestoreClient _firestore;
    private readonly FirebaseAuthClient _auth;

    public CloudStore(FirestoreClient firestore, FirebaseAuthClient auth)
    {
        _firestore = firestore;
        _auth = auth;
    }

    private string Uid => _auth.Session?.Uid
        ?? throw new FirebaseAuthException("NOT_SIGNED_IN", "Not signed in to EmuSync.");

    private string UserPath => $"users/{Uid}";

    // ------------------------------------------------------------- settings

    /// <summary>Loads the shared settings, creating the document on first sign-in.</summary>
    public async Task<CloudConfig> LoadConfigAsync(CancellationToken ct = default)
    {
        var fields = await _firestore.GetDocumentAsync(UserPath, ct);
        var session = _auth.Session!;

        if (fields == null)
        {
            var fresh = new CloudConfig
            {
                Email = session.Email,
                DisplayName = session.DisplayName
            };
            await SaveConfigAsync(fresh, ct);
            return fresh;
        }

        var config = new CloudConfig
        {
            Email = FirestoreClient.GetString(fields, "email", session.Email),
            DisplayName = FirestoreClient.GetString(fields, "displayName", session.DisplayName),
            AutoSync = FirestoreClient.GetBool(fields, "autoSync", true),
            RemoteCheckMinutes = FirestoreClient.GetInt(fields, "remoteCheckMinutes", 15)
        };

        foreach (var item in FirestoreClient.GetList(fields, "emulators"))
        {
            if (item is not Dictionary<string, object?> map) continue;
            string key = FirestoreClient.GetString(map, "key");
            if (string.IsNullOrWhiteSpace(key)) continue;

            var info = EmulatorCatalog.Resolve(key);
            config.Emulators.Add(new CloudEmulator
            {
                Key = key,
                DisplayName = FirestoreClient.GetString(map, "displayName", info.DisplayName),
                Console = FirestoreClient.GetString(map, "console", info.Console),
                Enabled = FirestoreClient.GetBool(map, "enabled", true),
                AddedUtc = FirestoreClient.GetDateTime(map, "addedUtc") ?? DateTime.UtcNow
            });
        }

        return config;
    }

    public async Task SaveConfigAsync(CloudConfig config, CancellationToken ct = default)
    {
        var fields = new Dictionary<string, object?>
        {
            ["email"] = config.Email,
            ["displayName"] = config.DisplayName,
            ["autoSync"] = config.AutoSync,
            ["remoteCheckMinutes"] = config.RemoteCheckMinutes,
            ["updatedUtc"] = DateTime.UtcNow,
            ["emulators"] = config.Emulators.Select(e => (object?)new Dictionary<string, object?>
            {
                ["key"] = e.Key,
                ["displayName"] = e.DisplayName,
                ["console"] = e.Console,
                ["enabled"] = e.Enabled,
                ["addedUtc"] = e.AddedUtc
            }).ToList()
        };

        await _firestore.PatchDocumentAsync(UserPath, fields, ct);
    }

    // -------------------------------------------------------------- devices

    /// <summary>Records this device and the folders it uses, so another PC can show them.</summary>
    public async Task SaveDeviceAsync(AppConfig local, CancellationToken ct = default)
    {
        var paths = local.LocalPaths.ToDictionary(kv => kv.Key, kv => (object?)kv.Value);

        await _firestore.PatchDocumentAsync($"{UserPath}/devices/{local.DeviceId}", new Dictionary<string, object?>
        {
            ["name"] = local.DeviceName,
            ["os"] = Environment.OSVersion.VersionString,
            ["lastSeenUtc"] = DateTime.UtcNow,
            ["paths"] = paths
        }, ct);
    }

    public async Task<List<CloudDevice>> ListDevicesAsync(CancellationToken ct = default)
    {
        var devices = new List<CloudDevice>();
        foreach (var (id, fields) in await _firestore.ListDocumentsAsync($"{UserPath}/devices", ct))
        {
            var device = new CloudDevice
            {
                Id = id,
                Name = FirestoreClient.GetString(fields, "name", id),
                Os = FirestoreClient.GetString(fields, "os"),
                LastSeenUtc = FirestoreClient.GetDateTime(fields, "lastSeenUtc")
            };
            foreach (var kv in FirestoreClient.GetMap(fields, "paths"))
                if (kv.Value is string path) device.Paths[kv.Key] = path;
            devices.Add(device);
        }
        return devices;
    }

    // ---------------------------------------------------------- file index

    public async Task<RemoteIndex> LoadIndexAsync(string emulatorKey, CancellationToken ct = default)
    {
        var index = new RemoteIndex { EmulatorKey = emulatorKey };
        var fields = await _firestore.GetDocumentAsync($"{UserPath}/emulators/{emulatorKey}", ct);
        if (fields == null) return index;

        index.LastSyncUtc = FirestoreClient.GetDateTime(fields, "lastSyncUtc");
        index.LastDeviceId = FirestoreClient.GetString(fields, "lastDeviceId");

        foreach (var item in FirestoreClient.GetList(fields, "files"))
        {
            if (item is not Dictionary<string, object?> map) continue;
            string path = FirestoreClient.GetString(map, "p");
            if (string.IsNullOrEmpty(path)) continue;

            index.Entries[path] = new IndexEntry
            {
                Path = path,
                Md5 = FirestoreClient.GetString(map, "md5"),
                Size = map.TryGetValue("sz", out var sz) && sz is long size ? size : 0,
                ModifiedUtc = FirestoreClient.GetDateTime(map, "mt") ?? DateTime.MinValue,
                DriveId = FirestoreClient.GetString(map, "id")
            };
        }

        return index;
    }

    public async Task SaveIndexAsync(RemoteIndex index, string deviceId, CancellationToken ct = default)
    {
        var files = index.Entries.Values
            .OrderBy(e => e.Path, StringComparer.OrdinalIgnoreCase)
            .Select(e => (object?)new Dictionary<string, object?>
            {
                ["p"] = e.Path,
                ["md5"] = e.Md5,
                ["sz"] = e.Size,
                ["mt"] = e.ModifiedUtc,
                ["id"] = e.DriveId
            })
            .ToList();

        index.LastSyncUtc = DateTime.UtcNow;
        index.LastDeviceId = deviceId;

        await _firestore.PatchDocumentAsync($"{UserPath}/emulators/{index.EmulatorKey}", new Dictionary<string, object?>
        {
            ["lastSyncUtc"] = index.LastSyncUtc,
            ["lastDeviceId"] = deviceId,
            ["fileCount"] = files.Count,
            ["files"] = files
        }, ct);
    }

    public async Task DeleteIndexAsync(string emulatorKey, CancellationToken ct = default) =>
        await _firestore.DeleteDocumentAsync($"{UserPath}/emulators/{emulatorKey}", ct);

    // ------------------------------------------------------------- profiles

    /// <summary>
    /// Merges the three sources into the list the UI works with: the cloud
    /// emulator list, the local paths of this device and the last sync time from
    /// each emulator index.
    /// </summary>
    public async Task<List<EmulatorProfile>> BuildProfilesAsync(CloudConfig config, AppConfig local,
        CancellationToken ct = default)
    {
        var profiles = new List<EmulatorProfile>();

        foreach (var emulator in config.Emulators)
        {
            var profile = new EmulatorProfile
            {
                Key = emulator.Key,
                DisplayName = emulator.DisplayName,
                Console = emulator.Console,
                Enabled = emulator.Enabled,
                LocalPath = local.GetLocalPath(emulator.Key) ?? ""
            };

            try
            {
                var fields = await _firestore.GetDocumentAsync($"{UserPath}/emulators/{emulator.Key}", ct);
                if (fields != null) profile.LastSyncUtc = FirestoreClient.GetDateTime(fields, "lastSyncUtc");
            }
            catch (IOException)
            {
                // The last sync time is cosmetic: never block the UI for it.
            }

            profiles.Add(profile);
        }

        return profiles;
    }
}
