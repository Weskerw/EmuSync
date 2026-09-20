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

/// <summary>One file listed in the shared index (for display on other devices).</summary>
public class IndexEntry
{
    /// <summary>Path relative to the emulator's save folder, '/' separated.</summary>
    public string Path { get; set; } = "";

    public string Md5 { get; set; } = "";
    public long Size { get; set; }
    public DateTime ModifiedUtc { get; set; }
}

/// <summary>A file that was deleted on purpose, so the other devices delete it too.</summary>
public class Tombstone
{
    public string Path { get; set; } = "";

    /// <summary>Content the file had when it was deleted: a device holding something
    /// different has newer work and must keep (and re-upload) it instead.</summary>
    public string Md5 { get; set; } = "";

    public DateTime DeletedUtc { get; set; }

    /// <summary>Device that performed the deletion, for the log.</summary>
    public string DeviceId { get; set; } = "";
}

/// <summary>
/// What the cloud knows about one emulator, stored in
/// <c>users/{uid}/emulators/{key}</c>: a summary of the synced saves plus the
/// tombstones that let deletions travel between devices.
///
/// It deliberately does NOT drive the merge — that is the job of the per-device
/// snapshot in <see cref="DeviceIndexStore"/>. Here the file list exists so other
/// devices (and, later, a phone) can show what is in sync without listing Drive.
/// </summary>
public class RemoteIndex
{
    /// <summary>Above this many files the list is truncated, to stay well under
    /// Firestore's 1 MiB document limit. Only the display list is affected.</summary>
    public const int MaxListedFiles = 1500;

    /// <summary>Tombstones are pruned after this long: by then every device has synced.</summary>
    public static readonly TimeSpan TombstoneLifetime = TimeSpan.FromDays(90);

    public string EmulatorKey { get; set; } = "";
    public DateTime? LastSyncUtc { get; set; }
    public string LastDeviceId { get; set; } = "";
    public int FileCount { get; set; }
    public long TotalBytes { get; set; }

    /// <summary>Synced files, possibly truncated — informational only.</summary>
    public List<IndexEntry> Files { get; set; } = new();

    /// <summary>relative path -> deletion record</summary>
    public Dictionary<string, Tombstone> Deleted { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public bool Truncated => FileCount > Files.Count;

    /// <summary>Drops expired tombstones so the document cannot grow forever.</summary>
    public void PruneTombstones()
    {
        DateTime cutoff = DateTime.UtcNow - TombstoneLifetime;
        foreach (string path in Deleted.Where(kv => kv.Value.DeletedUtc < cutoff).Select(kv => kv.Key).ToList())
            Deleted.Remove(path);
    }
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
        index.FileCount = FirestoreClient.GetInt(fields, "fileCount");
        index.TotalBytes = fields.TryGetValue("totalBytes", out var total) && total is long bytes ? bytes : 0;

        foreach (var item in FirestoreClient.GetList(fields, "files"))
        {
            if (item is not Dictionary<string, object?> map) continue;
            string path = FirestoreClient.GetString(map, "p");
            if (string.IsNullOrEmpty(path)) continue;

            index.Files.Add(new IndexEntry
            {
                Path = path,
                Md5 = FirestoreClient.GetString(map, "md5"),
                Size = map.TryGetValue("sz", out var sz) && sz is long size ? size : 0,
                ModifiedUtc = FirestoreClient.GetDateTime(map, "mt") ?? DateTime.MinValue
            });
        }

        foreach (var item in FirestoreClient.GetList(fields, "deleted"))
        {
            if (item is not Dictionary<string, object?> map) continue;
            string path = FirestoreClient.GetString(map, "p");
            if (string.IsNullOrEmpty(path)) continue;

            index.Deleted[path] = new Tombstone
            {
                Path = path,
                Md5 = FirestoreClient.GetString(map, "md5"),
                DeletedUtc = FirestoreClient.GetDateTime(map, "at") ?? DateTime.UtcNow,
                DeviceId = FirestoreClient.GetString(map, "dev")
            };
        }

        // Prune on the way in as well: an expired tombstone must not be acted on
        // one last time by the very run that removes it.
        index.PruneTombstones();
        return index;
    }

    public async Task SaveIndexAsync(RemoteIndex index, string deviceId, CancellationToken ct = default)
    {
        index.PruneTombstones();
        index.LastSyncUtc = DateTime.UtcNow;
        index.LastDeviceId = deviceId;

        var listed = index.Files
            .OrderBy(e => e.Path, StringComparer.OrdinalIgnoreCase)
            .Take(RemoteIndex.MaxListedFiles)
            .Select(e => (object?)new Dictionary<string, object?>
            {
                ["p"] = e.Path,
                ["md5"] = e.Md5,
                ["sz"] = e.Size,
                ["mt"] = e.ModifiedUtc
            })
            .ToList();

        var tombstones = index.Deleted.Values
            .OrderByDescending(t => t.DeletedUtc)
            .Take(RemoteIndex.MaxListedFiles)
            .Select(t => (object?)new Dictionary<string, object?>
            {
                ["p"] = t.Path,
                ["md5"] = t.Md5,
                ["at"] = t.DeletedUtc,
                ["dev"] = t.DeviceId
            })
            .ToList();

        await _firestore.PatchDocumentAsync($"{UserPath}/emulators/{index.EmulatorKey}", new Dictionary<string, object?>
        {
            ["lastSyncUtc"] = index.LastSyncUtc,
            ["lastDeviceId"] = deviceId,
            ["fileCount"] = index.FileCount,
            ["totalBytes"] = index.TotalBytes,
            ["files"] = listed,
            ["deleted"] = tombstones
        }, ct);
    }

    public async Task DeleteIndexAsync(string emulatorKey, CancellationToken ct = default) =>
        await _firestore.DeleteDocumentAsync($"{UserPath}/emulators/{emulatorKey}", ct);

    // ------------------------------------------------------------- activity

    /// <summary>
    /// Appends one sync to the history in <c>users/{uid}/activity</c>. Failures
    /// are swallowed by the caller: a missing history line must never turn a
    /// successful sync into an error.
    /// </summary>
    public async Task SaveRunAsync(SyncRunLog run, CancellationToken ct = default)
    {
        var operations = run.Operations
            .Take(SyncRunLog.MaxOperations)
            .Select(o => (object?)new Dictionary<string, object?>
            {
                ["a"] = o.Action.ToString(),
                ["p"] = o.Path,
                ["sz"] = o.Size,
                ["md5"] = o.Md5,
                ["at"] = o.AtUtc
            })
            .ToList();

        var fields = new Dictionary<string, object?>
        {
            ["emulatorKey"] = run.EmulatorKey,
            ["emulatorName"] = run.EmulatorName,
            ["console"] = run.Console,
            ["deviceId"] = run.DeviceId,
            ["deviceName"] = run.DeviceName,
            ["startedUtc"] = run.StartedUtc,
            ["durationMs"] = run.DurationMs,
            ["uploaded"] = run.Uploaded,
            ["downloaded"] = run.Downloaded,
            ["skipped"] = run.Skipped,
            ["conflicts"] = run.Conflicts,
            ["deletedLocal"] = run.DeletedLocal,
            ["deletedRemote"] = run.DeletedRemote,
            ["bytes"] = run.Bytes,
            ["truncated"] = run.Operations.Count > operations.Count,
            ["error"] = run.Error,
            ["ops"] = operations
        };

        await _firestore.PatchDocumentAsync($"{UserPath}/activity/{run.Id}", fields, ct);
    }

    /// <summary>The most recent syncs, newest first, across every device.</summary>
    public async Task<List<SyncRunLog>> ListRunsAsync(int limit = 200, CancellationToken ct = default)
    {
        var documents = await _firestore.RunQueryAsync(
            UserPath, "activity", orderByField: "startedUtc", descending: true, limit: limit, ct: ct);

        return documents.Select(d => ReadRun(d.Id, d.Fields)).ToList();
    }

    private static SyncRunLog ReadRun(string id, Dictionary<string, object?> fields)
    {
        var run = new SyncRunLog
        {
            Id = id,
            EmulatorKey = FirestoreClient.GetString(fields, "emulatorKey"),
            EmulatorName = FirestoreClient.GetString(fields, "emulatorName"),
            Console = FirestoreClient.GetString(fields, "console"),
            DeviceId = FirestoreClient.GetString(fields, "deviceId"),
            DeviceName = FirestoreClient.GetString(fields, "deviceName"),
            StartedUtc = FirestoreClient.GetDateTime(fields, "startedUtc") ?? DateTime.MinValue,
            DurationMs = FirestoreClient.GetInt(fields, "durationMs"),
            Uploaded = FirestoreClient.GetInt(fields, "uploaded"),
            Downloaded = FirestoreClient.GetInt(fields, "downloaded"),
            Skipped = FirestoreClient.GetInt(fields, "skipped"),
            Conflicts = FirestoreClient.GetInt(fields, "conflicts"),
            DeletedLocal = FirestoreClient.GetInt(fields, "deletedLocal"),
            DeletedRemote = FirestoreClient.GetInt(fields, "deletedRemote"),
            Bytes = fields.TryGetValue("bytes", out var b) && b is long bytes ? bytes : 0,
            Truncated = FirestoreClient.GetBool(fields, "truncated"),
            Error = fields.TryGetValue("error", out var e) ? e as string : null
        };

        foreach (var item in FirestoreClient.GetList(fields, "ops"))
        {
            if (item is not Dictionary<string, object?> map) continue;
            run.Operations.Add(new SyncOperation
            {
                Action = Enum.TryParse(FirestoreClient.GetString(map, "a"), out SyncAction action)
                    ? action
                    : SyncAction.Uploaded,
                Path = FirestoreClient.GetString(map, "p"),
                Size = map.TryGetValue("sz", out var sz) && sz is long size ? size : 0,
                Md5 = FirestoreClient.GetString(map, "md5"),
                AtUtc = FirestoreClient.GetDateTime(map, "at") ?? run.StartedUtc
            });
        }

        return run;
    }

    /// <summary>
    /// Deletes history older than the retention period. Called once per start, a
    /// batch at a time, so a long-unused account catches up over a few launches
    /// instead of firing hundreds of deletes at once.
    /// </summary>
    public async Task<int> PruneRunsAsync(int maxDeletes = 100, CancellationToken ct = default)
    {
        var expired = await _firestore.RunQueryAsync(
            UserPath, "activity",
            orderByField: "startedUtc", descending: false, limit: maxDeletes,
            where: ("startedUtc", "LESS_THAN", DateTime.UtcNow - SyncRunLog.Retention),
            ct: ct);

        foreach (var (id, _) in expired)
            await _firestore.DeleteDocumentAsync($"{UserPath}/activity/{id}", ct);

        return expired.Count;
    }

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
