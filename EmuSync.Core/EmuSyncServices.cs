namespace EmuSync.Core;

/// <summary>
/// Composition root: wires Firebase (identity + configuration) to Google Drive
/// (the save files themselves) and exposes the handful of operations the UI
/// needs. Keeping it here rather than in the WinForms project means an Android
/// front-end can reuse the whole flow and only supply its own
/// <see cref="IGoogleAuthorizationProvider"/>.
/// </summary>
public class EmuSyncServices : IDisposable
{
    public EmuSyncServices(IGoogleAuthorizationProvider googleAuth, ISecretProtector? protector = null,
        FirebaseOptions? firebaseOptions = null)
    {
        Firebase = firebaseOptions ?? FirebaseOptions.Load();
        Auth = new FirebaseAuthClient(Firebase, protector);
        Firestore = new FirestoreClient(Firebase, Auth);
        Cloud = new CloudStore(Firestore, Auth);
        Drive = new GoogleDriveClient(googleAuth);
        Local = AppConfig.Load();
        Profiles = Local.ProfilesFromCache();

        // Until Firestore answers, the cached settings ARE the configuration:
        // this keeps the menu checkbox and the timers agreeing with each other
        // while offline or before the first load.
        Config = new CloudConfig
        {
            AutoSync = Local.AutoSync,
            RemoteCheckMinutes = Local.RemoteCheckMinutes
        };
    }

    public FirebaseOptions Firebase { get; }
    public FirebaseAuthClient Auth { get; }
    public FirestoreClient Firestore { get; }
    public CloudStore Cloud { get; }
    public GoogleDriveClient Drive { get; }

    /// <summary>Device-specific settings (local folders, cache).</summary>
    public AppConfig Local { get; }

    /// <summary>Shared settings, available after <see cref="LoadCloudAsync"/>.</summary>
    public CloudConfig Config { get; private set; } = new();

    /// <summary>The emulators to sync, merged from cloud config and local paths.</summary>
    public List<EmulatorProfile> Profiles { get; private set; }

    public bool IsSignedIn => Auth.IsSignedIn;
    public string AccountLabel => Auth.Session?.Email ?? "not signed in";

    // ------------------------------------------------------------- sign-in

    /// <summary>Silently restores the previous session (no browser, no prompts).</summary>
    public async Task<bool> TryRestoreSessionAsync(CancellationToken ct = default)
    {
        if (!Firebase.IsConfigured) return false;
        return await Auth.RestoreSessionAsync(ct) != null;
    }

    public Task SignUpAsync(string email, string password, CancellationToken ct = default) =>
        Auth.SignUpAsync(email, password, ct);

    public Task SignInWithPasswordAsync(string email, string password, CancellationToken ct = default) =>
        Auth.SignInWithPasswordAsync(email, password, ct);

    public Task SendPasswordResetAsync(string email, CancellationToken ct = default) =>
        Auth.SendPasswordResetAsync(email, ct);

    /// <summary>
    /// One consent for both services: the Drive authorization also yields the
    /// Google ID token, which is exchanged for an EmuSync (Firebase) session.
    /// </summary>
    public async Task SignInWithGoogleAsync(bool forceAccountPicker = false, CancellationToken ct = default)
    {
        // Always take a fresh ID token: one captured earlier in the session would
        // already be past its one-hour life and Firebase would reject it.
        string? idToken = forceAccountPicker
            ? await ReauthorizeForIdTokenAsync(ct)
            : await Drive.RefreshIdTokenAsync(ct);

        if (string.IsNullOrEmpty(idToken))
            throw new FirebaseAuthException("NO_ID_TOKEN",
                "Google did not return an ID token: check that the OAuth client requests the " +
                "'openid' scope and belongs to the same project as Firebase.");

        await Auth.SignInWithGoogleAsync(idToken, ct);
    }

    private async Task<string?> ReauthorizeForIdTokenAsync(CancellationToken ct)
    {
        await Drive.ReauthorizeAsync(ct);
        return Drive.LastIdToken;
    }

    /// <summary>Connects Drive on its own (email/password users, or after a token reset).</summary>
    public Task ConnectDriveAsync(CancellationToken ct = default) => Drive.ConnectAsync(ct);

    /// <summary>Signs out of everything and forgets both tokens.</summary>
    public void SignOut()
    {
        Auth.SignOut();
        Drive.SignOut();
        Profiles = new List<EmulatorProfile>();

        // Keep the user's own preferences rather than snapping back to the
        // defaults: the menu still shows them and they apply on the next sign-in.
        Config = new CloudConfig
        {
            AutoSync = Local.AutoSync,
            RemoteCheckMinutes = Local.RemoteCheckMinutes
        };
    }

    // --------------------------------------------------------- cloud config

    /// <summary>Loads the shared configuration and registers this device.</summary>
    public async Task LoadCloudAsync(CancellationToken ct = default)
    {
        Config = await Cloud.LoadConfigAsync(ct);
        Profiles = await Cloud.BuildProfilesAsync(Config, Local, ct);
        Local.CacheFromCloud(Config, Profiles);
        Local.Save();
        await Cloud.SaveDeviceAsync(Local, ct);
    }

    /// <summary>Adds an emulator to the shared list and links it to a local folder here.</summary>
    public async Task AddEmulatorAsync(EmulatorInfo emulator, string localPath, CancellationToken ct = default)
    {
        var existing = Config.Find(emulator.Key);
        if (existing == null)
        {
            Config.Emulators.Add(new CloudEmulator
            {
                Key = emulator.Key,
                DisplayName = emulator.DisplayName,
                Console = emulator.Console
            });
        }
        else
        {
            existing.Enabled = true;
        }

        LinkLocalPath(emulator.Key, localPath);
        Local.Save();

        await Cloud.SaveConfigAsync(Config, ct);
        await Cloud.SaveDeviceAsync(Local, ct);
        await RefreshProfilesAsync(ct);
    }

    /// <summary>Points an already-enrolled emulator at a folder on this device.</summary>
    public async Task SetLocalPathAsync(string key, string localPath, CancellationToken ct = default)
    {
        LinkLocalPath(key, localPath);
        Local.Save();
        await Cloud.SaveDeviceAsync(Local, ct);
        await RefreshProfilesAsync(ct);
    }

    /// <summary>
    /// Stops syncing an emulator. <paramref name="everywhere"/> removes it from
    /// the shared configuration; otherwise it is only unlinked from this device.
    /// Files on disk and on Drive are never touched.
    /// </summary>
    public async Task RemoveEmulatorAsync(string key, bool everywhere, CancellationToken ct = default)
    {
        Local.SetLocalPath(key, null);
        Local.Save();

        // The snapshot describes a folder we no longer watch: keeping it would
        // make a future re-add look like a mass deletion.
        DeviceIndexStore.Delete(key);

        if (everywhere)
        {
            Config.Emulators.RemoveAll(e => string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase));
            await Cloud.SaveConfigAsync(Config, ct);
            await Cloud.DeleteIndexAsync(key, ct);
        }

        await Cloud.SaveDeviceAsync(Local, ct);
        await RefreshProfilesAsync(ct);
    }

    public async Task SaveSettingsAsync(bool autoSync, int remoteCheckMinutes, CancellationToken ct = default)
    {
        Config.AutoSync = autoSync;
        Config.RemoteCheckMinutes = remoteCheckMinutes;
        Local.AutoSync = autoSync;
        Local.RemoteCheckMinutes = remoteCheckMinutes;
        Local.Save();
        await Cloud.SaveConfigAsync(Config, ct);
    }

    public async Task RefreshProfilesAsync(CancellationToken ct = default)
    {
        Profiles = await Cloud.BuildProfilesAsync(Config, Local, ct);
        Local.CacheFromCloud(Config, Profiles);
        Local.Save();
    }

    /// <summary>
    /// Emulators detected on this machine that are not enrolled yet — what the
    /// wizard offers on first run and the "Detect emulators" command re-offers later.
    /// </summary>
    public List<DetectedEmulator> DetectNewEmulators() =>
        EmulatorCatalog.Detect()
            .Where(d => Config.Find(d.Emulator.Key) == null || Local.GetLocalPath(d.Emulator.Key) == null)
            .ToList();

    /// <summary>
    /// Emulators enrolled from another device but not yet linked here. Autodetect
    /// is tried first, so moving to a new PC usually needs no manual setup.
    /// </summary>
    public List<EmulatorProfile> UnlinkedProfiles() => Profiles.Where(p => !p.IsLinkedHere).ToList();

    /// <summary>Tries to find a local folder for every enrolled-but-unlinked emulator.</summary>
    public async Task<int> AutoLinkProfilesAsync(CancellationToken ct = default)
    {
        int linked = 0;
        var detected = EmulatorCatalog.Detect().ToDictionary(d => d.Emulator.Key, d => d.LocalPath,
            StringComparer.OrdinalIgnoreCase);

        foreach (var profile in UnlinkedProfiles())
        {
            if (!detected.TryGetValue(profile.Key, out string? path)) continue;
            LinkLocalPath(profile.Key, path);
            linked++;
        }

        if (linked > 0)
        {
            Local.Save();
            await Cloud.SaveDeviceAsync(Local, ct);
            await RefreshProfilesAsync(ct);
        }
        return linked;
    }

    /// <summary>
    /// Points an emulator at a folder on this device. The single place that does
    /// so, because every such change must drop the sync snapshot: it describes the
    /// contents of the *previous* folder, and reading it against a new one looks
    /// exactly like the user having deleted every save.
    /// </summary>
    private void LinkLocalPath(string key, string localPath)
    {
        if (!string.Equals(Local.GetLocalPath(key), localPath, StringComparison.OrdinalIgnoreCase))
            DeviceIndexStore.Delete(key);

        Local.SetLocalPath(key, localPath);
    }

    public SyncEngine CreateEngine() => new(Drive, Cloud, Local.DeviceId, Local.DeviceName);

    /// <summary>The sync history of every device, newest first.</summary>
    public Task<List<SyncRunLog>> LoadHistoryAsync(int limit = 200, CancellationToken ct = default) =>
        Cloud.ListRunsAsync(limit, ct);

    /// <summary>
    /// Drops history past its retention period. Best-effort and silent: it runs in
    /// the background at startup and nothing depends on it.
    /// </summary>
    public async Task PruneHistoryAsync(CancellationToken ct = default)
    {
        try { await Cloud.PruneRunsAsync(ct: ct); }
        catch { /* offline, or rules refused: try again next launch */ }
    }

    public void Dispose() => Drive.Dispose();
}
