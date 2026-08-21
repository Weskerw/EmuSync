using EmuSync.Core;

namespace EmuSync;

public class MainForm : Form
{
    private readonly AppConfig _config = AppConfig.Load();
    private readonly GoogleDriveClient _drive = new();

    private readonly ListView _list = new();
    private readonly TextBox _log = new();
    private readonly Button _btnAdd = new();
    private readonly Button _btnRemove = new();
    private readonly Button _btnSyncSelected = new();
    private readonly Button _btnSyncAll = new();
    private readonly CheckBox _chkAuto = new();

    // Automatic sync: one watcher per folder (event-driven, ~zero cost)
    // + quiet period so we don't sync while the emulator is still writing.
    private static readonly TimeSpan QuietPeriod = TimeSpan.FromSeconds(30);
    private readonly List<FileSystemWatcher> _watchers = new();
    private readonly HashSet<SyncProfile> _dirtyProfiles = new();
    private DateTime _lastChangeUtc;
    private readonly System.Windows.Forms.Timer _autoSyncTimer = new() { Interval = 10_000 };
    private readonly System.Windows.Forms.Timer _remoteCheckTimer = new();
    private bool _syncing;

    // System tray icon; when launched with --minimized (Start with Windows) the
    // window stays hidden and the app lives in the tray.
    private readonly NotifyIcon _tray = new();
    private readonly bool _startMinimized;
    private readonly bool _firstRun = !AppConfig.ConfigFileExists;

    public MainForm(bool startMinimized = false)
    {
        _startMinimized = startMinimized;
        Text = "EmuSync – Sync emulator saves with Google Drive";
        MinimumSize = new Size(720, 480);
        StartPosition = FormStartPosition.CenterScreen;
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { /* keep default */ }

        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.MultiSelect = false;
        _list.Dock = DockStyle.Fill;
        _list.Columns.Add("Profile", 150);
        _list.Columns.Add("Local folder", 330);
        _list.Columns.Add("Last sync", 160);

        _btnAdd.Text = "Add folder...";
        _btnRemove.Text = "Remove";
        _btnSyncSelected.Text = "Sync selected";
        _btnSyncAll.Text = "Sync all";

        _btnAdd.Click += (_, _) => AddProfile();
        _btnRemove.Click += (_, _) => RemoveProfile();
        _btnSyncSelected.Click += async (_, _) => await SyncAsync(onlySelected: true);
        _btnSyncAll.Click += async (_, _) => await SyncAsync(onlySelected: false);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(4)
        };
        foreach (var b in new[] { _btnAdd, _btnRemove, _btnSyncSelected, _btnSyncAll })
        {
            b.AutoSize = true;
            buttons.Controls.Add(b);
        }

        _chkAuto.Text = "Auto-sync when saves change";
        _chkAuto.AutoSize = true;
        _chkAuto.Margin = new Padding(12, 6, 3, 3);
        _chkAuto.Checked = _config.AutoSync;
        _chkAuto.CheckedChanged += (_, _) =>
        {
            _config.AutoSync = _chkAuto.Checked;
            _config.Save();
            Log(_chkAuto.Checked ? "Auto-sync enabled." : "Auto-sync disabled.");
        };
        buttons.Controls.Add(_chkAuto);

        _log.Multiline = true;
        _log.ReadOnly = true;
        _log.ScrollBars = ScrollBars.Vertical;
        _log.Dock = DockStyle.Fill;
        _log.Font = new Font(FontFamily.GenericMonospace, 9f);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal
        };
        split.Panel1.Controls.Add(_list);
        split.Panel2.Controls.Add(_log);
        // Set after layout, otherwise it may exceed the container's limits.
        Shown += (_, _) => { if (split.Height > 120) split.SplitterDistance = split.Height / 2; };

        var menu = new MenuStrip();
        var settingsMenu = new ToolStripMenuItem("Settings");
        var changeAccount = new ToolStripMenuItem("Change Google account...");
        changeAccount.Click += async (_, _) => await ChangeAccountAsync();
        settingsMenu.DropDownItems.Add(changeAccount);

        var startWithWindows = new ToolStripMenuItem("Start with Windows")
        {
            CheckOnClick = true,
            Checked = StartupManager.IsEnabled()
        };
        startWithWindows.CheckedChanged += (_, _) =>
        {
            try
            {
                StartupManager.SetEnabled(startWithWindows.Checked);
                Log(startWithWindows.Checked
                    ? "EmuSync will start automatically with Windows."
                    : "Automatic startup with Windows disabled.");
            }
            catch (Exception ex)
            {
                Log("ERROR (startup setting): " + ex.Message);
            }
        };
        settingsMenu.DropDownItems.Add(startWithWindows);

        menu.Items.Add(settingsMenu);
        MainMenuStrip = menu;

        Controls.Add(split);
        Controls.Add(buttons);
        Controls.Add(menu);

        RefreshList();
        RebuildWatchers();

        _autoSyncTimer.Tick += async (_, _) => await AutoSyncTickAsync();
        _autoSyncTimer.Start();

        // Periodic Drive check to pick up changes made on other PCs.
        if (_config.RemoteCheckMinutes > 0)
        {
            _remoteCheckTimer.Interval = Math.Max(5, _config.RemoteCheckMinutes) * 60_000;
            _remoteCheckTimer.Tick += async (_, _) => await RemoteCheckTickAsync();
            _remoteCheckTimer.Start();
        }

        // System tray icon with quick actions.
        _tray.Icon = Icon ?? SystemIcons.Application;
        _tray.Text = "EmuSync";
        var trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Open EmuSync", null, (_, _) => ShowFromTray());
        trayMenu.Items.Add("Sync all now", null, async (_, _) => await SyncAsync(onlySelected: false));
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("Exit", null, (_, _) => Close());
        _tray.ContextMenuStrip = trayMenu;
        _tray.DoubleClick += (_, _) => ShowFromTray();
        _tray.Visible = true;

        if (_startMinimized)
        {
            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;
        }

        // On startup: first-run wizard or sign-in if needed, then sync all profiles.
        Shown += async (_, _) => await StartupAsync();
    }

    private void ShowFromTray()
    {
        Show();
        ShowInTaskbar = true;
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private async Task StartupAsync()
    {
        if (_startMinimized) Hide();

        if (_firstRun && !_startMinimized)
        {
            // Guided setup: welcome → Google sign-in → choose folders.
            using (var wizard = new FirstRunWizard(_config, _drive,
                       Path.Combine(AppContext.BaseDirectory, "credentials.json")))
            {
                wizard.ShowDialog(this);
            }
            _config.Save(); // marks the first run as done even if the wizard was cancelled
            RefreshList();
            RebuildWatchers();
        }

        if (!_drive.IsConnected && !GoogleDriveClient.HasStoredToken)
        {
            // Not signed in (wizard skipped/cancelled, or token deleted).
            Log(_startMinimized
                ? "Not signed in to Google: open the window and press a Sync button to sign in."
                : "Sign-in not completed: press a Sync button whenever you want to connect Google Drive.");
            return;
        }

        SetBusy(true);
        try
        {
            await EnsureConnectedAsync();

            if (_config.Profiles.Count == 0)
            {
                Log("No folders configured yet: use 'Add folder...' to get started.");
                return;
            }

            Log("Automatic sync on startup...");
            await RunSyncAsync(_config.Profiles.ToList());
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

    private async Task ChangeAccountAsync()
    {
        if (_syncing)
        {
            MessageBox.Show(this, "A sync is in progress. Please wait until it finishes.",
                "EmuSync", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (MessageBox.Show(this,
                "Sign out from the current Google account?\n" +
                "Your browser will open to sign in with another account.",
                "EmuSync", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
            return;

        _drive.SignOut();
        Log("Signed out from the Google account.");

        SetBusy(true);
        try
        {
            await EnsureConnectedAsync();
            if (_config.Profiles.Count > 0)
            {
                Log("Syncing with the new account...");
                await RunSyncAsync(_config.Profiles.ToList());
            }
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

    /// <summary>One FileSystemWatcher per profile: the OS notifies changes, no polling.</summary>
    private void RebuildWatchers()
    {
        foreach (var w in _watchers) w.Dispose();
        _watchers.Clear();

        foreach (var profile in _config.Profiles)
        {
            if (!Directory.Exists(profile.LocalPath)) continue;
            var w = new FileSystemWatcher(profile.LocalPath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
            };
            var p = profile; // capture for the lambda
            FileSystemEventHandler handler = (_, e) => OnFolderChanged(p, e.FullPath);
            w.Changed += handler;
            w.Created += handler;
            w.Deleted += handler;
            w.Renamed += (_, e) => OnFolderChanged(p, e.FullPath);
            w.EnableRaisingEvents = true;
            _watchers.Add(w);
        }
    }

    private void OnFolderChanged(SyncProfile profile, string fullPath)
    {
        // Ignore our own temp files and events generated by the sync itself.
        if (fullPath.EndsWith(".emusync-tmp", StringComparison.OrdinalIgnoreCase)) return;
        if (_syncing || !IsHandleCreated) return;

        BeginInvoke(() =>
        {
            _dirtyProfiles.Add(profile);
            _lastChangeUtc = DateTime.UtcNow;
        });
    }

    /// <summary>
    /// Periodic check: syncs all profiles to pick up changes that arrived on
    /// Drive from other PCs. Minimal cost: one listing per profile and, if
    /// nothing changed (same MD5), no transfers at all.
    /// </summary>
    private async Task RemoteCheckTickAsync()
    {
        if (!_config.AutoSync || _syncing || _config.Profiles.Count == 0) return;
        // Never open the browser from a timer: only if already connected or with a stored token.
        if (!_drive.IsConnected && !GoogleDriveClient.HasStoredToken) return;
        // If there are fresh local changes (emulator writing), let the local
        // auto-sync handle them and try again on the next tick.
        if (_dirtyProfiles.Count > 0) return;

        SetBusy(true);
        try
        {
            await EnsureConnectedAsync();
            Log("Periodic Google Drive check...");
            await RunSyncAsync(_config.Profiles.ToList());
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
        if (!_config.AutoSync || _syncing || _dirtyProfiles.Count == 0) return;
        if (DateTime.UtcNow - _lastChangeUtc < QuietPeriod) return; // wait for the folder to settle

        var targets = _dirtyProfiles.ToList();
        _dirtyProfiles.Clear();

        SetBusy(true);
        try
        {
            await EnsureConnectedAsync();
            Log($"Changes detected in {targets.Count} profile(s): automatic sync...");
            await RunSyncAsync(targets);
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

    private void RefreshList()
    {
        _list.Items.Clear();
        foreach (var p in _config.Profiles)
        {
            string lastSync = p.LastSyncUtc.HasValue
                ? p.LastSyncUtc.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                : "never";
            _list.Items.Add(new ListViewItem(new[] { p.Name, p.LocalPath, lastSync }) { Tag = p });
        }
    }

    private void AddProfile()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Choose the emulator's saves / memory card folder"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        string suggested = new DirectoryInfo(dlg.SelectedPath).Name;
        string? name = PromptForName(suggested);
        if (string.IsNullOrWhiteSpace(name)) return;
        name = name.Trim();

        if (_config.Profiles.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(this, "A profile with this name already exists.", "EmuSync",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _config.Profiles.Add(new SyncProfile { Name = name, LocalPath = dlg.SelectedPath });
        _config.Save();
        RefreshList();
        RebuildWatchers();
        Log($"Added profile '{name}' → {dlg.SelectedPath}");
    }

    private string? PromptForName(string suggested)
    {
        using var form = new Form
        {
            Text = "Profile name",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(360, 110),
            MinimizeBox = false,
            MaximizeBox = false
        };
        var label = new Label { Text = "Name (e.g. PCSX2, Dolphin...):", Left = 10, Top = 10, AutoSize = true };
        var box = new TextBox { Left = 10, Top = 32, Width = 340, Text = suggested };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 194, Top = 70, Width = 75 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 275, Top = 70, Width = 75 };
        form.Controls.AddRange(new Control[] { label, box, ok, cancel });
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        return form.ShowDialog(this) == DialogResult.OK ? box.Text : null;
    }

    private void RemoveProfile()
    {
        if (_list.SelectedItems.Count == 0) return;
        var profile = (SyncProfile)_list.SelectedItems[0].Tag!;
        if (MessageBox.Show(this,
                $"Remove profile '{profile.Name}'?\n(Files on disk and on Drive are NOT touched.)",
                "EmuSync", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        _config.Profiles.Remove(profile);
        _dirtyProfiles.Remove(profile);
        _config.Save();
        RefreshList();
        RebuildWatchers();
    }

    private async Task SyncAsync(bool onlySelected)
    {
        List<SyncProfile> targets;
        if (onlySelected)
        {
            if (_list.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "Select a profile from the list first.", "EmuSync",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            targets = new List<SyncProfile> { (SyncProfile)_list.SelectedItems[0].Tag! };
        }
        else
        {
            targets = _config.Profiles.ToList();
        }

        if (targets.Count == 0)
        {
            MessageBox.Show(this, "Add at least one folder first.", "EmuSync",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SetBusy(true);
        try
        {
            await EnsureConnectedAsync();
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

    private async Task EnsureConnectedAsync()
    {
        if (_drive.IsConnected) return;
        Log(GoogleDriveClient.HasStoredToken
            ? "Connecting to Google Drive..."
            : "Connecting to Google Drive: your browser will open for sign-in...");
        await _drive.ConnectAsync(Path.Combine(AppContext.BaseDirectory, "credentials.json"));
        Log("Connected.");
    }

    private async Task RunSyncAsync(List<SyncProfile> targets)
    {
        var engine = new SyncEngine(_drive);
        foreach (var profile in targets)
        {
            try
            {
                await engine.SyncProfileAsync(profile, Log);
                profile.LastSyncUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                Log($"ERROR in profile '{profile.Name}': {ex.Message}");
            }
        }
        _config.Save();
        RefreshList();
        Log("Synchronization finished.");
    }

    private void SetBusy(bool busy)
    {
        _syncing = busy;
        foreach (var b in new[] { _btnAdd, _btnRemove, _btnSyncSelected, _btnSyncAll })
            b.Enabled = !busy;
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
        _drive.Dispose();
        base.OnFormClosed(e);
    }
}
