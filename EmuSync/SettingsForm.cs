using System.Diagnostics;
using EmuSync.Core;

namespace EmuSync;

/// <summary>
/// The settings window: sections on the left, controls on the right.
///
/// Everything applies as soon as you change it — there is no OK/Cancel, because
/// half of these settings live in the cloud and the other half in the Windows
/// registry, and pretending they could be rolled back together would be a lie.
///
/// The layout lives in SettingsForm.Designer.cs; this file only holds the behaviour.
/// </summary>
public partial class SettingsForm : Form
{
    private readonly EmuSyncServices _services;
    private readonly Action<string> _log;

    /// <summary>True while the controls are being filled in, so handlers stay quiet.</summary>
    private bool _loading;

    /// <summary>Set when something changed that the main window has to pick up.</summary>
    public bool ConfigurationChanged { get; private set; }

    /// <summary>Parameterless constructor required by the Visual Studio designer.</summary>
    private SettingsForm()
    {
        InitializeComponent();
        _services = null!;
        _log = _ => { };
    }

    public SettingsForm(EmuSyncServices services, Action<string> log)
    {
        InitializeComponent();

        _services = services;
        _log = log;

        _sections.SelectedIndexChanged += (_, _) => ShowSection(_sections.SelectedIndex);
        _close.Click += (_, _) => Close();

        // General
        _chkAutoSync.CheckedChanged += async (_, _) => await SaveGeneralAsync();
        _chkRemoteCheck.CheckedChanged += async (_, _) =>
        {
            _numRemoteMinutes.Enabled = _chkRemoteCheck.Checked;
            await SaveGeneralAsync();
        };
        _numRemoteMinutes.ValueChanged += async (_, _) => await SaveGeneralAsync();
        _chkStartup.CheckedChanged += (_, _) => ApplyStartup();

        // Account
        _btnSignOut.Click += (_, _) => SignOut();
        _btnChangeDrive.Click += async (_, _) => await ChangeDriveAccountAsync();
        _btnDriveFolder.Click += async (_, _) => await ChangeDriveFolderAsync();

        // Emulators
        _btnAdd.Click += async (_, _) => await AddAsync();
        _btnSetFolder.Click += async (_, _) => await SetFolderAsync();
        _btnRemove.Click += async (_, _) => await RemoveAsync();
        _btnDetect.Click += async (_, _) => await DetectAsync();
        _emulators.SelectedIndexChanged += (_, _) => UpdateEmulatorButtons();
        _emulators.DoubleClick += async (_, _) => await SetFolderAsync();

        // Info
        _linkSite.LinkClicked += (_, _) => OpenUrl("https://emusync-43d2b.web.app/");
        _linkGitHub.LinkClicked += (_, _) => OpenUrl("https://github.com/Weskerw/EmuSync");
        _linkConfigFolder.LinkClicked += (_, _) => OpenUrl(AppConfig.ConfigDir);

        LoadValues();
        _sections.SelectedIndex = 0;
    }

    private void ShowSection(int index)
    {
        _pageGeneral.Visible = index == 0;
        _pageAccount.Visible = index == 1;
        _pageEmulators.Visible = index == 2;
        _pageInfo.Visible = index == 3;
    }

    private void LoadValues()
    {
        _loading = true;
        try
        {
            _chkAutoSync.Checked = _services.Config.AutoSync;

            int minutes = _services.Config.RemoteCheckMinutes;
            _chkRemoteCheck.Checked = minutes > 0;
            _numRemoteMinutes.Enabled = minutes > 0;
            _numRemoteMinutes.Value = Math.Clamp(minutes <= 0 ? 15 : minutes,
                (int)_numRemoteMinutes.Minimum, (int)_numRemoteMinutes.Maximum);

            _chkStartup.Checked = StartupManager.IsEnabled();

            string version = typeof(SettingsForm).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
            _lblVersion.Text = $"EmuSync {version}";
            _lblPaths.Text = $"Settings and tokens: {AppConfig.ConfigDir}\r\n" +
                             $"Local trash for deleted saves: {Path.Combine(AppConfig.ConfigDir, "trash")}";
        }
        finally
        {
            _loading = false;
        }

        RefreshAccount();
        RefreshEmulators();
    }

    private void RefreshAccount()
    {
        _lblAccountEmail.Text = _services.IsSignedIn
            ? _services.AccountLabel
            : "Not signed in.";
        _btnSignOut.Enabled = _services.IsSignedIn;

        _lblDriveState.Text = _services.Drive.IsConnected
            ? "Connected. The saves are stored in your own Drive."
            : _services.Drive.HasStoredToken
                ? "Authorized: it will connect at the next sync."
                : "Not connected.";

        _lblDriveFolder.Text = $"My Drive / {_services.Config.DriveFolder.Replace("/", " / ")}";
    }

    private void RefreshEmulators()
    {
        _emulators.BeginUpdate();
        _emulators.Items.Clear();

        foreach (var profile in _services.Profiles)
        {
            var item = new ListViewItem(new[]
            {
                profile.DisplayName,
                profile.Console,
                profile.IsLinkedHere ? profile.LocalPath : "(not configured on this PC)"
            })
            { Tag = profile };

            if (!profile.IsLinkedHere) item.ForeColor = SystemColors.GrayText;
            _emulators.Items.Add(item);
        }

        _emulators.EndUpdate();
        UpdateEmulatorButtons();
    }

    private void UpdateEmulatorButtons()
    {
        // Every one of these writes to the shared configuration, so they need an
        // account: without one they would just produce an error dialog.
        bool signedIn = _services.IsSignedIn;
        bool selected = _emulators.SelectedItems.Count > 0;

        _btnAdd.Enabled = signedIn;
        _btnDetect.Enabled = signedIn;
        _btnSetFolder.Enabled = signedIn && selected;
        _btnRemove.Enabled = signedIn && selected;
        _btnChangeDrive.Enabled = signedIn;
        _btnDriveFolder.Enabled = signedIn;
    }

    private EmulatorProfile? Selected() =>
        _emulators.SelectedItems.Count == 0 ? null : (EmulatorProfile)_emulators.SelectedItems[0].Tag!;

    // ------------------------------------------------------------- general

    private async Task SaveGeneralAsync()
    {
        if (_loading) return;

        int minutes = _chkRemoteCheck.Checked ? (int)_numRemoteMinutes.Value : 0;
        _services.Config.AutoSync = _chkAutoSync.Checked;
        _services.Config.RemoteCheckMinutes = minutes;
        ConfigurationChanged = true;

        try
        {
            if (_services.IsSignedIn)
                await _services.SaveSettingsAsync(_chkAutoSync.Checked, minutes);
            else
            {
                _services.Local.AutoSync = _chkAutoSync.Checked;
                _services.Local.RemoteCheckMinutes = minutes;
                _services.Local.Save();
            }
        }
        catch (Exception ex)
        {
            _log("ERROR saving the settings: " + ex.Message);
        }
    }

    private void ApplyStartup()
    {
        if (_loading) return;

        try
        {
            StartupManager.SetEnabled(_chkStartup.Checked);
            _log(_chkStartup.Checked
                ? "EmuSync will start automatically with Windows."
                : "Automatic startup with Windows disabled.");
        }
        catch (Exception ex)
        {
            _log("ERROR (startup setting): " + ex.Message);
            MessageBox.Show(this, ex.Message, "EmuSync", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _loading = true;
            _chkStartup.Checked = StartupManager.IsEnabled();
            _loading = false;
        }
    }

    // ------------------------------------------------------------- account

    private void SignOut()
    {
        if (MessageBox.Show(this,
                "Sign out of EmuSync on this PC?\n" +
                "Nothing is deleted: your settings stay in your account and your saves stay on Drive.",
                "EmuSync", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
            return;

        _services.SignOut();
        _log("Signed out.");
        ConfigurationChanged = true;
        RefreshAccount();
        RefreshEmulators();
    }

    private async Task ChangeDriveAccountAsync()
    {
        if (await EmulatorActions.ChangeDriveAccountAsync(this, _services, _log))
        {
            ConfigurationChanged = true;
            RefreshAccount();
        }
    }

    private async Task ChangeDriveFolderAsync()
    {
        if (await EmulatorActions.ChangeDriveFolderAsync(this, _services, _log))
        {
            ConfigurationChanged = true;
            RefreshAccount();
        }
    }

    // ----------------------------------------------------------- emulators

    private async Task AddAsync()
    {
        if (await EmulatorActions.AddAsync(this, _services, _log)) AfterEmulatorChange();
    }

    private async Task SetFolderAsync()
    {
        var profile = Selected();
        if (profile == null) return;
        if (await EmulatorActions.SetFolderAsync(this, _services, profile, _log)) AfterEmulatorChange();
    }

    private async Task RemoveAsync()
    {
        var profile = Selected();
        if (profile == null) return;
        if (await EmulatorActions.RemoveAsync(this, _services, profile, _log)) AfterEmulatorChange();
    }

    private async Task DetectAsync()
    {
        if (await EmulatorActions.DetectAsync(this, _services, _log)) AfterEmulatorChange();
    }

    private void AfterEmulatorChange()
    {
        ConfigurationChanged = true;
        RefreshEmulators();
    }

    // ---------------------------------------------------------------- info

    private void OpenUrl(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _log("Could not open " + target + ": " + ex.Message);
        }
    }
}
