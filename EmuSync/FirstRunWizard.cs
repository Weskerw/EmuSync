using EmuSync.Core;

namespace EmuSync;

/// <summary>
/// Guided first-run setup: welcome → EmuSync account → Google Drive → emulators.
///
/// The Drive step disappears when the user signed in with Google, because that
/// single consent already granted Drive access.
/// </summary>
public class FirstRunWizard : Form
{
    private readonly EmuSyncServices _services;

    private readonly Label _title = new();
    private readonly Label _body = new();
    private readonly Button _btnNext = new();
    private readonly Button _btnCancel = new();

    // Step 1 – account
    private readonly Button _btnSignIn = new();
    private readonly Label _accountStatus = new();

    // Step 2 – Drive
    private readonly Button _btnDrive = new();
    private readonly Label _driveStatus = new();

    // Step 3 – emulators
    private readonly CheckedListBox _emulatorList = new();
    private readonly Button _btnAddManual = new();
    private readonly Button _btnRedetect = new();
    private readonly CheckBox _chkStartup = new();

    private readonly List<DetectedEmulator> _detected = new();
    private int _step;

    public FirstRunWizard(EmuSyncServices services)
    {
        _services = services;

        Text = "EmuSync – First-run setup";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(560, 380);

        _title.Font = new Font(Font.FontFamily, 12f, FontStyle.Bold);
        _title.SetBounds(20, 15, 520, 28);
        _body.SetBounds(20, 48, 520, 76);

        _btnSignIn.Text = "Sign in / create account";
        _btnSignIn.SetBounds(20, 140, 200, 34);
        _btnSignIn.Click += (_, _) => SignIn();
        _accountStatus.SetBounds(232, 146, 308, 60);

        _btnDrive.Text = "Connect Google Drive";
        _btnDrive.SetBounds(20, 140, 200, 34);
        _btnDrive.Click += async (_, _) => await ConnectDriveAsync();
        _driveStatus.SetBounds(232, 146, 308, 60);

        _emulatorList.SetBounds(20, 134, 400, 150);
        _emulatorList.CheckOnClick = true;
        _btnAddManual.Text = "Add manually...";
        _btnAddManual.SetBounds(430, 134, 110, 30);
        _btnAddManual.Click += (_, _) => AddManually();
        _btnRedetect.Text = "Detect again";
        _btnRedetect.SetBounds(430, 170, 110, 30);
        _btnRedetect.Click += (_, _) => RunDetection();

        _chkStartup.Text = "Start EmuSync automatically with Windows (in the tray)";
        _chkStartup.SetBounds(20, 292, 520, 24);
        _chkStartup.Checked = true;

        _btnNext.SetBounds(360, 334, 90, 30);
        _btnNext.Click += async (_, _) => await NextStepAsync();
        _btnCancel.Text = "Cancel";
        _btnCancel.SetBounds(455, 334, 90, 30);
        _btnCancel.Click += (_, _) => Close();

        Controls.AddRange(new Control[]
        {
            _title, _body, _btnSignIn, _accountStatus, _btnDrive, _driveStatus,
            _emulatorList, _btnAddManual, _btnRedetect, _chkStartup, _btnNext, _btnCancel
        });

        ShowStep(0);
    }

    private void ShowStep(int step)
    {
        _step = step;
        _btnSignIn.Visible = _accountStatus.Visible = step == 1;
        _btnDrive.Visible = _driveStatus.Visible = step == 2;
        _emulatorList.Visible = _btnAddManual.Visible = _btnRedetect.Visible = _chkStartup.Visible = step == 3;

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

    private void SignIn()
    {
        using var login = new LoginForm(_services);
        if (login.ShowDialog(this) != DialogResult.OK) return;

        _accountStatus.Text = $"Signed in as {_services.AccountLabel}";
        _btnNext.Enabled = true;
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
        }
        catch (Exception ex)
        {
            _driveStatus.Text = "Error: " + ex.Message;
            _btnDrive.Enabled = true;
        }
    }

    private void RunDetection()
    {
        _emulatorList.Items.Clear();
        _detected.Clear();

        UseWaitCursor = true;
        try
        {
            _detected.AddRange(EmulatorCatalog.Detect());
        }
        finally
        {
            UseWaitCursor = false;
        }

        foreach (var item in _detected)
        {
            _emulatorList.Items.Add($"{item.Emulator.DisplayName} ({item.Emulator.Console})  →  {item.LocalPath}", true);
        }

        if (_detected.Count == 0)
            _emulatorList.Items.Add("No emulator detected – use \"Add manually...\"", false);
    }

    private void AddManually()
    {
        var used = _detected.Select(d => d.Emulator.Key).ToList();
        using var dlg = new AddEmulatorDialog(used);
        if (dlg.ShowDialog(this) != DialogResult.OK || dlg.SelectedEmulator == null) return;

        // Drop the "nothing detected" placeholder, if present.
        if (_detected.Count == 0 && _emulatorList.Items.Count == 1) _emulatorList.Items.Clear();

        _detected.Add(new DetectedEmulator(dlg.SelectedEmulator, dlg.SelectedPath));
        _emulatorList.Items.Add(
            $"{dlg.SelectedEmulator.DisplayName} ({dlg.SelectedEmulator.Console})  →  {dlg.SelectedPath}", true);
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

                for (int i = 0; i < _detected.Count; i++)
                {
                    if (!_emulatorList.GetItemChecked(i)) continue;
                    await _services.AddEmulatorAsync(_detected[i].Emulator, _detected[i].LocalPath);
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
