using EmuSync.Core;

namespace EmuSync;

/// <summary>
/// Guided first-run setup: welcome → EmuSync account → Google Drive → emulators.
/// The layout lives in FirstRunWizard.Designer.cs (one panel per step); this file
/// only holds the behaviour.
///
/// The Drive step disappears when the user signed in with Google, because that
/// single consent already granted Drive access.
/// </summary>
public partial class FirstRunWizard : Form
{
    private readonly EmuSyncServices _services;

    /// <summary>What the last step offers: detected emulators plus manual additions.</summary>
    private readonly List<DetectedEmulator> _entries = new();

    /// <summary>Keys added by hand, kept across a re-detection.</summary>
    private readonly HashSet<string> _manualKeys = new(StringComparer.OrdinalIgnoreCase);

    private int _step;

    /// <summary>Parameterless constructor required by the Visual Studio designer.</summary>
    private FirstRunWizard()
    {
        InitializeComponent();
        _services = null!;
    }

    public FirstRunWizard(EmuSyncServices services)
    {
        InitializeComponent();
        _services = services;

        _btnSignIn.Click += async (_, _) => await SignInAsync();
        _btnDrive.Click += async (_, _) => await ConnectDriveAsync();
        _btnAddManual.Click += (_, _) => AddManually();
        _btnRedetect.Click += (_, _) => RunDetection();
        _btnNext.Click += async (_, _) => await NextStepAsync();
        _btnCancel.Click += (_, _) => Close();

        ShowStep(0);
    }

    private void ShowStep(int step)
    {
        _step = step;
        _panelAccount.Visible = step == 1;
        _panelDrive.Visible = step == 2;
        _panelEmulators.Visible = step == 3;

        switch (step)
        {
            case 0:
                _title.Text = "Welcome to EmuSync!";
                _body.Text = "EmuSync keeps your emulator saves in sync across your computers.\n\n" +
                             "Your settings live in your EmuSync account, while the save files stay " +
                             "in your own Google Drive, grouped by emulator.";
                _btnNext.Text = "Next >";
                _btnNext.Enabled = true;
                break;

            case 1:
                _title.Text = "Step 1 of 3 – Your EmuSync account";
                _body.Text = "Sign in with Google or with an email address. The account is what lets " +
                             "your other computers (and, in the future, your phone) pick up the same " +
                             "configuration.";
                _accountStatus.Text = _services.IsSignedIn ? $"Signed in as {_services.AccountLabel}" : "";
                _btnNext.Text = "Next >";
                _btnNext.Enabled = _services.IsSignedIn;
                break;

            case 2:
                _title.Text = "Step 2 of 3 – Google Drive";
                _body.Text = "The save files are stored in your Google Drive, under the EmuSync folder. " +
                             "They never leave your account and never count against anybody else's quota.";
                _driveStatus.Text = _services.Drive.IsConnected ? "Connected!" : "";
                _btnNext.Text = "Next >";
                _btnNext.Enabled = _services.Drive.IsConnected;
                break;

            case 3:
                _title.Text = "Step 3 of 3 – Your emulators";
                _body.Text = "These are the emulators found on this computer. Tick the ones to sync; " +
                             "each one gets its own folder on Drive (PCSX2 → EmuSync/pcsx2).";
                _btnNext.Text = "Finish";
                _btnNext.Enabled = true;
                if (_emulatorList.Items.Count == 0) RunDetection();
                break;
        }
    }

    private async Task NextStepAsync()
    {
        // Google sign-in already granted Drive: skip the step entirely.
        if (_step == 1 && _services.Drive.IsConnected)
        {
            ShowStep(3);
            return;
        }

        if (_step < 3)
        {
            ShowStep(_step + 1);
            return;
        }

        await FinishAsync();
    }

    private async Task SignInAsync()
    {
        using var login = new LoginForm(_services);
        if (login.ShowDialog(this) != DialogResult.OK) return;

        _accountStatus.Text = $"Signed in as {_services.AccountLabel}";
        _btnNext.Enabled = true;

        // Nothing left to do on this step: don't make the user press Next to
        // confirm something that already happened.
        await NextStepAsync();
    }

    private async Task ConnectDriveAsync()
    {
        _btnDrive.Enabled = false;
        _driveStatus.Text = "Waiting for the browser...";
        try
        {
            await _services.ConnectDriveAsync();
            _driveStatus.Text = "Connected!";
            _btnNext.Enabled = true;
            await NextStepAsync();
        }
        catch (Exception ex)
        {
            _driveStatus.Text = "Error: " + ex.Message;
            _btnDrive.Enabled = true;
        }
    }

    private void RunDetection()
    {
        // Whatever the user added by hand must survive a re-scan.
        var manual = _entries.Where(e => _manualKeys.Contains(e.Emulator.Key)).ToList();
        _entries.Clear();

        UseWaitCursor = true;
        try
        {
            _entries.AddRange(EmulatorCatalog.Detect().Where(d => !_manualKeys.Contains(d.Emulator.Key)));
        }
        finally
        {
            UseWaitCursor = false;
        }

        _entries.AddRange(manual);
        RefreshEmulatorList();
    }

    private void RefreshEmulatorList()
    {
        _emulatorList.Items.Clear();

        foreach (var item in _entries)
            _emulatorList.Items.Add($"{item.Emulator.DisplayName} ({item.Emulator.Console})  →  {item.LocalPath}", true);

        if (_entries.Count == 0)
            _emulatorList.Items.Add("No emulator detected – use \"Add manually...\"", false);
    }

    private void AddManually()
    {
        using var dlg = new AddEmulatorDialog(_entries.Select(d => d.Emulator.Key));
        if (dlg.ShowDialog(this) != DialogResult.OK || dlg.SelectedEmulator == null) return;

        _manualKeys.Add(dlg.SelectedEmulator.Key);
        _entries.Add(new DetectedEmulator(dlg.SelectedEmulator, dlg.SelectedPath));
        RefreshEmulatorList();
    }

    private async Task FinishAsync()
    {
        _btnNext.Enabled = _btnCancel.Enabled = false;
        UseWaitCursor = true;
        try
        {
            if (_services.IsSignedIn)
            {
                await _services.LoadCloudAsync();

                for (int i = 0; i < _entries.Count && i < _emulatorList.Items.Count; i++)
                {
                    if (!_emulatorList.GetItemChecked(i)) continue;
                    await _services.AddEmulatorAsync(_entries[i].Emulator, _entries[i].LocalPath);
                }
            }

            try
            {
                StartupManager.SetEnabled(_chkStartup.Checked);
            }
            catch
            {
                // Non-fatal: the option can still be toggled later from the Settings menu.
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "EmuSync", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _btnNext.Enabled = _btnCancel.Enabled = true;
        }
        finally
        {
            UseWaitCursor = false;
        }
    }
}
