using EmuSync.Core;

namespace EmuSync;

/// <summary>
/// Main window: what is being synced, and what happened. The visual part (menu,
/// toolbar, list, log, tray icon, timers) lives in MainForm.Designer.cs so it can
/// be opened in the Visual Studio designer; this file only holds the behaviour.
///
/// Two accounts are involved: the EmuSync account (Firebase — identity and
/// configuration) and Google Drive (the save files). A Google sign-in covers
/// both at once.
/// </summary>
public partial class MainForm : Form
{
    private readonly EmuSyncServices _services;

    // Automatic sync: one watcher per folder (event-driven, ~zero cost)
    // + quiet period so we don't sync while the emulator is still writing.
    private static readonly TimeSpan QuietPeriod = TimeSpan.FromSeconds(30);
    private readonly List<FileSystemWatcher> _watchers = new();
    private readonly HashSet<string> _dirtyKeys = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Folders that changed while a sync was running (possibly by that very sync).</summary>
    private readonly HashSet<string> _changedDuringSync = new(StringComparer.OrdinalIgnoreCase);

    private DateTime _lastChangeUtc;

    /// <summary>True while any long operation runs: greys out the toolbar.</summary>
    private bool _syncing;

    /// <summary>
    /// True only while files are actually being transferred. Distinct from
    /// <see cref="_syncing"/>, which also covers dialogs and cloud loads: a save
    /// written while a login window is open is a real change and must not be
    /// mistaken for an echo of our own downloads.
    /// </summary>
    private bool _inSync;

    // When launched with --minimized (Start with Windows) the window stays
    // hidden and the app lives in the tray.
    private readonly bool _startMinimized;

    /// <summary>Set when a sign-in expired: timer-driven syncs pause until the user signs in again.</summary>
    private bool _needsSignIn;
    private readonly bool _firstRun = !AppConfig.ConfigFileExists;

    /// <summary>Parameterless constructor required by the Visual Studio designer.</summary>
    public MainForm() : this(false) { }

    public MainForm(bool startMinimized)
    {
        InitializeComponent();

        _services = new EmuSyncServices(
            new DesktopGoogleAuthProvider(Path.Combine(AppContext.BaseDirectory, "credentials.json")),
            new DpapiProtector());

        _startMinimized = startMinimized;
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { /* keep default */ }

        // --- menu ---
        _miSettings.Click += async (_, _) => await ShowSettingsAsync();
        _miExit.Click += (_, _) => Close();

        // --- toolbar ---
        _tbSyncAll.Click += async (_, _) => await SyncAsync(onlySelected: false);
        _tbSyncSelected.Click += async (_, _) => await SyncAsync(onlySelected: true);
        _tbAdd.Click += async (_, _) => await AddEmulatorAsync();
        _tbSetFolder.Click += async (_, _) => await SetLocalFolderAsync();
        _tbRemove.Click += async (_, _) => await RemoveEmulatorAsync();
        _tbHistory.Click += async (_, _) => await ShowHistoryAsync();

        _list.SelectedIndexChanged += (_, _) => UpdateToolbarState();
        _list.DoubleClick += async (_, _) => await SetLocalFolderAsync();

        // Split the window evenly after layout: doing it in the designer may
        // exceed the container's limits at other DPI/sizes.
        Shown += (_, _) => { if (_split.Height > 120) _split.SplitterDistance = _split.Height / 2; };

        RefreshList();
        RebuildWatchers();
        UpdateStatusBar();

        _autoSyncTimer.Tick += async (_, _) => await AutoSyncTickAsync();
        _autoSyncTimer.Start();

        _remoteCheckTimer.Tick += async (_, _) => await RemoteCheckTickAsync();
        ApplyRemoteCheckInterval();

        // --- System tray icon with quick actions ---
        _tray.Icon = Icon ?? SystemIcons.Application;
        _trayOpen.Click += (_, _) => ShowFromTray();
        _traySyncAll.Click += async (_, _) => await SyncAsync(onlySelected: false);
        _trayExit.Click += (_, _) => Close();
        _tray.DoubleClick += (_, _) => ShowFromTray();

        if (_startMinimized)
        {
            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;
        }

        // On startup: first-run wizard or sign-in if needed, then sync everything.
        Shown += async (_, _) => await StartupAsync();
    }

    /// <summary>Toolbar buttons cannot carry shortcuts, so the window handles them.</summary>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.F5:
                _ = SyncAsync(onlySelected: true);
                return true;
            case Keys.Control | Keys.F5:
                _ = SyncAsync(onlySelected: false);
                return true;
            case Keys.Control | Keys.H:
                _ = ShowHistoryAsync();
                return true;
            case Keys.Control | Keys.N:
                _ = AddEmulatorAsync();
                return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void ShowFromTray()
    {
        Show();
        ShowInTaskbar = true;
        WindowState = FormWindowState.Normal;
        Activate();
    }

    // --------------------------------------------------------------- startup

    private async Task StartupAsync()
    {
        if (_startMinimized) Hide();

        if (!_services.Firebase.IsConfigured)
        {
            Log($"Firebase is not configured: add {FirebaseOptions.FileName} next to EmuSync.exe (see the README).");
            return;
        }

        if (_firstRun && !_startMinimized)
        {
            using (var wizard = new FirstRunWizard(_services))
            {
                wizard.ShowDialog(this);
            }
            _services.Local.Save(); // marks the first run as done even if the wizard was cancelled
            RefreshList();
            RebuildWatchers();
            UpdateStatusBar();
        }

        SetBusy(true);
        try
        {
            // 1. EmuSync account (silently, from the stored refresh token).
            if (!_services.IsSignedIn)
            {
                Log("Restoring your EmuSync session...");
                await _services.TryRestoreSessionAsync();
            }

            if (!_services.IsSignedIn)
            {
                if (_startMinimized)
                {
                    Log("Not signed in: open the window to sign in to EmuSync.");
                    NotifySignInRequired();
                    return;
                }

                using var login = new LoginForm(_services);
                if (login.ShowDialog(this) != DialogResult.OK)
                {
                    Log("Sign-in skipped: open Settings whenever you want to connect.");
                    return;
                }
            }

            Log($"Signed in as {_services.AccountLabel}.");
            UpdateStatusBar();

            // 2. Shared configuration.
            await ReloadCloudAsync();

            // Old history goes out in the background: nothing waits on it.
            _ = _services.PruneHistoryAsync();

            // 3. Drive (where the saves actually live).
            await EnsureDriveAsync();

            if (_services.Profiles.Count == 0)
            {
                Log("No emulator configured yet: use 'Add emulator...' or Settings > Emulators > Detect.");
                return;
            }

            Log("Automatic sync on startup...");
            await RunSyncAsync(SyncableProfiles(), interactive: !_startMinimized);
        }
        catch (Exception ex)
        {
            Log("ERROR: " + ex.Message);
            if (!_startMinimized)
                MessageBox.Show(this, ex.Message, "EmuSync", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>Reloads the cloud config and links any emulator detected here but configured elsewhere.</summary>
    private async Task ReloadCloudAsync()
    {
        await _services.LoadCloudAsync();

        int linked = await _services.AutoLinkProfilesAsync();
        if (linked > 0)
            Log($"{linked} emulator(s) configured on another device were matched to folders on this PC.");

        foreach (var profile in _services.UnlinkedProfiles())
            Log($"⚠ '{profile.DisplayName}' has no folder on this PC: select it and use 'Local folder...'");

        ApplyRemoteCheckInterval(); // the cloud may carry a different period than the cache
        RefreshList();
        RebuildWatchers();
        UpdateStatusBar();
    }

    /// <summary>(Re)starts the periodic check with the current setting, or stops it when disabled.</summary>
    private void ApplyRemoteCheckInterval()
    {
        int minutes = _services.Config.RemoteCheckMinutes;
        _remoteCheckTimer.Stop();
        if (minutes <= 0) return;

        _remoteCheckTimer.Interval = Math.Max(5, minutes) * 60_000;
        _remoteCheckTimer.Start();
    }

    private async Task EnsureDriveAsync()
    {
        if (_services.Drive.IsConnected) return;

        Log(_services.Drive.HasStoredToken
            ? "Connecting to Google Drive..."
            : "Connecting to Google Drive: your browser will open for sign-in...");
        await _services.ConnectDriveAsync();
        _needsSignIn = false;
        Log("Google Drive connected.");
        UpdateStatusBar();
    }

    // -------------------------------------------------------------- settings

    private async Task ShowSettingsAsync()
    {
        if (_syncing)
        {
            MessageBox.Show(this, "A sync is in progress. Please wait until it finishes.",
                "EmuSync", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var settings = new SettingsForm(_services, Log);
        settings.ShowDialog(this);

        if (!settings.ConfigurationChanged) return;

        // Everything in there applies immediately; the window only has to catch up.
        ApplyRemoteCheckInterval();
        RefreshList();
        RebuildWatchers();
        UpdateStatusBar();

        await Task.CompletedTask;
    }

    // -------------------------------------------------------------- emulators

    private async Task AddEmulatorAsync()
    {
        if (_syncing || !await RequireSignInAsync()) return;
        if (await EmulatorActions.AddAsync(this, _services, Log)) AfterEmulatorChange();
    }

    private async Task SetLocalFolderAsync()
    {
        if (_syncing) return; // reachable from the list's double-click even while busy

        var profile = SelectedProfile();
        if (profile == null) return;
        if (!await RequireSignInAsync()) return;

        if (await EmulatorActions.SetFolderAsync(this, _services, profile, Log)) AfterEmulatorChange();
    }

    private async Task RemoveEmulatorAsync()
    {
        if (_syncing) return;

        var profile = SelectedProfile();
        if (profile == null) return;

        if (await EmulatorActions.RemoveAsync(this, _services, profile, Log))
        {
            _dirtyKeys.Remove(profile.Key);
            AfterEmulatorChange();
        }
    }

    private void AfterEmulatorChange()
    {
        RefreshList();
        RebuildWatchers();
        UpdateStatusBar();
    }

    private async Task ShowHistoryAsync()
    {
        if (!await RequireSignInAsync()) return;

        using var history = new HistoryForm(_services);
        history.ShowDialog(this);
    }

    // ------------------------------------------------------------- automation

    /// <summary>One FileSystemWatcher per linked emulator: the OS notifies changes, no polling.</summary>
    private void RebuildWatchers()
    {
        foreach (var w in _watchers) w.Dispose();
        _watchers.Clear();

        foreach (var profile in _services.Profiles)
        {
            if (!profile.Enabled || !profile.IsLinkedHere) continue;
            if (!Directory.Exists(profile.LocalPath)) continue;

            var w = new FileSystemWatcher(profile.LocalPath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
            };
            string key = profile.Key; // capture for the lambda
            FileSystemEventHandler handler = (_, e) => OnFolderChanged(key, e.FullPath);
            w.Changed += handler;
            w.Created += handler;
            w.Deleted += handler;
            w.Renamed += (_, e) => OnFolderChanged(key, e.FullPath);
            w.EnableRaisingEvents = true;
            _watchers.Add(w);
        }
    }

    private void OnFolderChanged(string emulatorKey, string fullPath)
    {
        // Ignore our own temp files.
        if (fullPath.EndsWith(".emusync-tmp", StringComparison.OrdinalIgnoreCase)) return;
        if (!IsHandleCreated) return;

        try
        {
            BeginInvoke(() =>
            {
                // A sync that downloads files fires these events itself: recording
                // them during a sync would loop forever, so they are noted apart
                // and only kept when the sync turns out not to have written anything.
                if (_inSync) _changedDuringSync.Add(emulatorKey);
                else _dirtyKeys.Add(emulatorKey);
                _lastChangeUtc = DateTime.UtcNow;
            });
        }
        catch (InvalidOperationException)
        {
            // The window was closed between the check and the call: nothing to do.
            // (ObjectDisposedException derives from this one, so it is covered too.)
        }
    }

    /// <summary>
    /// Periodic check: syncs everything to pick up changes that arrived from other
    /// PCs. Minimal cost: one listing per emulator and, if nothing changed (same
    /// MD5), no transfers at all.
    /// </summary>
    private async Task RemoteCheckTickAsync()
    {
        if (!_services.Config.AutoSync || _syncing || !_services.IsSignedIn) return;
        if (_needsSignIn) return;                       // waiting for the user to sign in again
        if (!_services.Drive.IsConnected && !_services.Drive.HasStoredToken) return;
        if (_dirtyKeys.Count > 0) return;               // local changes first, on the next tick

        var targets = SyncableProfiles();
        if (targets.Count == 0) return;

        SetBusy(true);
        try
        {
            await EnsureDriveAsync();
            Log("Periodic check...");
            await RunSyncAsync(targets, interactive: false);
        }
        catch (Exception ex)
        {
            Log("ERROR (remote check): " + ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task AutoSyncTickAsync()
    {
        if (!_services.Config.AutoSync || _syncing || _dirtyKeys.Count == 0) return;
        if (_needsSignIn || !_services.IsSignedIn) return;
        if (DateTime.UtcNow - _lastChangeUtc < QuietPeriod) return; // wait for the folder to settle

        var keys = _dirtyKeys.ToList();
        _dirtyKeys.Clear();

        var targets = SyncableProfiles().Where(p => keys.Contains(p.Key, StringComparer.OrdinalIgnoreCase)).ToList();
        if (targets.Count == 0) return;

        SetBusy(true);
        try
        {
            await EnsureDriveAsync();
            Log($"Changes detected in {targets.Count} emulator(s): automatic sync...");
            await RunSyncAsync(targets, interactive: false);
        }
        catch (Exception ex)
        {
            Log("ERROR (auto-sync): " + ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    // ------------------------------------------------------------------- sync

    private List<EmulatorProfile> SyncableProfiles() =>
        _services.Profiles.Where(p => p.Enabled && p.IsLinkedHere).ToList();

    private EmulatorProfile? SelectedProfile() =>
        _list.SelectedItems.Count == 0 ? null : (EmulatorProfile)_list.SelectedItems[0].Tag!;

    private async Task SyncAsync(bool onlySelected)
    {
        // The tray menu and the list stay clickable while a sync runs, and a
        // re-entrant run would reset the mid-sync change tracking of the outer one.
        if (_syncing) return;
        if (!await RequireSignInAsync()) return;

        List<EmulatorProfile> targets;
        if (onlySelected)
        {
            var profile = SelectedProfile();
            if (profile == null)
            {
                MessageBox.Show(this, "Select an emulator from the list first.", "EmuSync",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!profile.IsLinkedHere)
            {
                MessageBox.Show(this, $"'{profile.DisplayName}' has no local folder on this PC.\n" +
                                      "Use 'Local folder...' first.", "EmuSync",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            targets = new List<EmulatorProfile> { profile };
        }
        else
        {
            targets = SyncableProfiles();
        }

        if (targets.Count == 0)
        {
            MessageBox.Show(this, "Add at least one emulator first.", "EmuSync",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SetBusy(true);
        try
        {
            await EnsureDriveAsync();
            await RunSyncAsync(targets);
        }
        catch (Exception ex)
        {
            Log("ERROR: " + ex.Message);
            MessageBox.Show(this, ex.Message, "EmuSync", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>
    /// Runs the sync. If Google rejects the stored token ('invalid_grant': expired
    /// or revoked), signs in again once and retries the emulator.
    /// <paramref name="interactive"/> is false for timer-driven syncs, where the
    /// browser must not pop up unannounced: there we just warn and stop retrying
    /// until the user syncs manually.
    /// </summary>
    private async Task RunSyncAsync(List<EmulatorProfile> targets, bool interactive = true)
    {
        const int MaxSignIns = 2; // never turn a broken token into a stream of browser windows
        var engine = _services.CreateEngine();
        int signIns = 0;
        bool abort = false;

        // Anything the watchers report from here on is either our own downloads or
        // a real change that arrived mid-sync; the two are told apart at the end.
        _changedDuringSync.Clear();
        var untouched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _inSync = true;

        try
        {
            foreach (var profile in targets)
            {
                if (abort) break;
                await SyncOneAsync(profile, engine, interactive, untouched);
            }
        }
        finally
        {
            _inSync = false;
        }

        // A save written by the emulator while the sync was running would otherwise
        // be lost until the next unrelated change. Folders this sync never wrote to
        // are safe to trust, and so are folders it did not even look at — only the
        // ones it downloaded into could be echoing its own writes back at us.
        var syncedKeys = new HashSet<string>(targets.Select(p => p.Key), StringComparer.OrdinalIgnoreCase);
        foreach (string key in _changedDuringSync)
            if (!syncedKeys.Contains(key) || untouched.Contains(key)) _dirtyKeys.Add(key);
        _changedDuringSync.Clear();

        // Always persist what did succeed, even if we gave up half-way.
        try
        {
            _services.Local.CacheFromCloud(_services.Config, _services.Profiles);
            _services.Local.Save();
        }
        catch { /* the cache is an optimization, never a reason to fail */ }

        RefreshList();
        Log("Synchronization finished.");

        // ---- local state shared by the loop above ----
        async Task SyncOneAsync(EmulatorProfile profile, SyncEngine syncEngine, bool canPrompt,
            HashSet<string> untouchedKeys)
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    var stats = await syncEngine.SyncProfileAsync(profile, Log);

                    // "Untouched" means nothing was written into the local folder,
                    // so any watcher event for it came from the emulator, not us.
                    if (stats.Downloaded == 0 && stats.DeletedLocal == 0) untouchedKeys.Add(profile.Key);
                    _needsSignIn = false; // the tokens work: resume automatic syncing
                    return;
                }
                catch (Exception ex) when (attempt == 0 && signIns < MaxSignIns &&
                                           GoogleDriveClient.IsInvalidGrant(ex))
                {
                    signIns++;
                    Log("The Google Drive authorization has expired or was revoked.");

                    if (!canPrompt)
                    {
                        _needsSignIn = true;
                        Log("Sign in again with 'Sync all' to resume automatic syncing.");
                        NotifySignInRequired();
                        abort = true;
                        break;
                    }

                    Log("Signing in again: your browser will open...");
                    try
                    {
                        await _services.Drive.ReauthorizeAsync();
                        Log("Signed in again: retrying...");
                    }
                    catch (Exception authEx)
                    {
                        _needsSignIn = true;
                        Log("ERROR: sign-in failed: " + authEx.Message);
                        abort = true;
                        break;
                    }
                }
                catch (FirebaseAuthException ex)
                {
                    _needsSignIn = ex.RequiresSignIn;
                    Log($"ERROR ({profile.DisplayName}): {ex.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    if (GoogleDriveClient.IsInvalidGrant(ex)) _needsSignIn = true;
                    Log($"ERROR ({profile.DisplayName}): {ex.Message}");
                    break;
                }
            }
        }
    }

    /// <summary>Makes sure there is an EmuSync session before touching the cloud.</summary>
    private async Task<bool> RequireSignInAsync()
    {
        if (_services.IsSignedIn) return true;

        if (!_services.Firebase.IsConfigured)
        {
            MessageBox.Show(this,
                $"Firebase is not configured: add {FirebaseOptions.FileName} next to EmuSync.exe (see the README).",
                "EmuSync", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (await _services.TryRestoreSessionAsync())
        {
            await ReloadCloudAsync();
            return true;
        }

        using var login = new LoginForm(_services);
        if (login.ShowDialog(this) != DialogResult.OK) return false;

        Log($"Signed in as {_services.AccountLabel}.");
        await ReloadCloudAsync();
        return true;
    }

    // -------------------------------------------------------------------- UI

    private void RefreshList()
    {
        _list.Items.Clear();
        foreach (var profile in _services.Profiles)
        {
            string lastSync = profile.LastSyncUtc.HasValue
                ? profile.LastSyncUtc.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                : "never";

            var item = new ListViewItem(new[]
            {
                profile.DisplayName,
                profile.Console,
                profile.IsLinkedHere ? profile.LocalPath : "(not configured on this PC)",
                lastSync
            })
            { Tag = profile };

            if (!profile.IsLinkedHere) item.ForeColor = SystemColors.GrayText;
            _list.Items.Add(item);
        }

        UpdateToolbarState();
    }

    /// <summary>Greys out what needs a selection, or a sync that is not already running.</summary>
    private void UpdateToolbarState()
    {
        bool selected = _list.SelectedItems.Count > 0;
        _tbSyncSelected.Enabled = !_syncing && selected;
        _tbSetFolder.Enabled = !_syncing && selected;
        _tbRemove.Enabled = !_syncing && selected;
        _tbSyncAll.Enabled = !_syncing;
        _tbAdd.Enabled = !_syncing;
        _traySyncAll.Enabled = !_syncing;
    }

    private void UpdateStatusBar()
    {
        _lblAccount.Text = _services.IsSignedIn ? $"Account: {_services.AccountLabel}" : "Not signed in";

        string state = _services.Drive.IsConnected
            ? "connected"
            : _services.Drive.HasStoredToken ? "authorized" : "not connected";
        _lblDrive.Text = $"Drive: {state} · folder {_services.Config.DriveFolder}";
    }

    private void NotifySignInRequired()
    {
        if (InvokeRequired) { BeginInvoke(() => NotifySignInRequired()); return; }
        try
        {
            _tray.BalloonTipTitle = "EmuSync";
            _tray.BalloonTipText = "The sign-in has expired. Open EmuSync and press 'Sync all'.";
            _tray.BalloonTipIcon = ToolTipIcon.Warning;
            _tray.ShowBalloonTip(10000);
        }
        catch { /* balloon tips are best-effort */ }
    }

    private void SetBusy(bool busy)
    {
        _syncing = busy;
        UpdateToolbarState();
        UseWaitCursor = busy;
    }

    private void Log(string message)
    {
        if (InvokeRequired) { BeginInvoke(() => Log(message)); return; }
        _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _autoSyncTimer.Stop();
        _autoSyncTimer.Dispose();
        _remoteCheckTimer.Stop();
        _remoteCheckTimer.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        foreach (var w in _watchers) w.Dispose();
        _services.Dispose();
        base.OnFormClosed(e);
    }
}
