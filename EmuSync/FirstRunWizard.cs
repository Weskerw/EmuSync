using EmuSync.Core;

namespace EmuSync;

/// <summary>Guided first-run setup: welcome → Google sign-in → choose folders.</summary>
public class FirstRunWizard : Form
{
    private readonly AppConfig _config;
    private readonly GoogleDriveClient _drive;
    private readonly string _credentialsPath;

    private readonly Label _title = new();
    private readonly Label _body = new();
    private readonly Button _btnNext = new();
    private readonly Button _btnCancel = new();

    // Step 1 controls
    private readonly Button _btnSignIn = new();
    private readonly Label _signInStatus = new();

    // Step 2 controls
    private readonly ListBox _folderList = new();
    private readonly Button _btnAddFolder = new();

    private int _step;

    public FirstRunWizard(AppConfig config, GoogleDriveClient drive, string credentialsPath)
    {
        _config = config;
        _drive = drive;
        _credentialsPath = credentialsPath;

        Text = "EmuSync – First-run setup";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(520, 340);

        _title.Font = new Font(Font.FontFamily, 12f, FontStyle.Bold);
        _title.SetBounds(20, 15, 480, 28);
        _body.SetBounds(20, 50, 480, 80);

        _btnSignIn.Text = "Sign in with Google";
        _btnSignIn.SetBounds(20, 145, 180, 32);
        _btnSignIn.Click += async (_, _) => await SignInAsync();
        _signInStatus.SetBounds(210, 151, 290, 60);

        _folderList.SetBounds(20, 140, 360, 130);
        _btnAddFolder.Text = "Add folder...";
        _btnAddFolder.SetBounds(390, 140, 110, 32);
        _btnAddFolder.Click += (_, _) => AddFolder();

        _btnNext.SetBounds(320, 295, 90, 30);
        _btnNext.Click += (_, _) => NextStep();
        _btnCancel.Text = "Cancel";
        _btnCancel.SetBounds(415, 295, 90, 30);
        _btnCancel.Click += (_, _) => Close();

        Controls.AddRange(new Control[]
        {
            _title, _body, _btnSignIn, _signInStatus, _folderList, _btnAddFolder, _btnNext, _btnCancel
        });

        ShowStep(0);
    }

    private void ShowStep(int step)
    {
        _step = step;
        _btnSignIn.Visible = _signInStatus.Visible = step == 1;
        _folderList.Visible = _btnAddFolder.Visible = step == 2;

        switch (step)
        {
            case 0:
                _title.Text = "Welcome to EmuSync!";
                _body.Text = "EmuSync keeps your emulator saves in sync with Google Drive.\n\n" +
                             "This quick setup will connect your Google account and let you " +
                             "choose the save folders to sync.";
                _btnNext.Text = "Next >";
                _btnNext.Enabled = true;
                break;
            case 1:
                _title.Text = "Step 1 of 2 – Connect Google Drive";
                _body.Text = "Press the button below: your browser will open so you can sign in " +
                             "with your Google account (needed only once).";
                _btnNext.Text = "Next >";
                _btnNext.Enabled = _drive.IsConnected;
                break;
            case 2:
                _title.Text = "Step 2 of 2 – Choose the folders to sync";
                _body.Text = "Add the folders where your emulators keep their saves or memory " +
                             "cards (e.g. PCSX2's 'memcards'). You can add more later.";
                _btnNext.Text = "Finish";
                _btnNext.Enabled = true;
                break;
        }
    }

    private void NextStep()
    {
        if (_step < 2)
        {
            ShowStep(_step + 1);
            return;
        }
        DialogResult = DialogResult.OK;
        Close();
    }

    private async Task SignInAsync()
    {
        _btnSignIn.Enabled = false;
        _signInStatus.Text = "Waiting for the browser...";
        try
        {
            await _drive.ConnectAsync(_credentialsPath);
            _signInStatus.Text = "Connected!";
            _btnNext.Enabled = true;
        }
        catch (Exception ex)
        {
            _signInStatus.Text = "Error: " + ex.Message;
            _btnSignIn.Enabled = true;
        }
    }

    private void AddFolder()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Choose the emulator's saves / memory card folder"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        // Use the folder name as profile name, made unique if needed.
        string baseName = new DirectoryInfo(dlg.SelectedPath).Name;
        string name = baseName;
        int i = 2;
        while (_config.Profiles.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            name = $"{baseName} ({i++})";

        _config.Profiles.Add(new SyncProfile { Name = name, LocalPath = dlg.SelectedPath });
        _config.Save();
        _folderList.Items.Add($"{name} → {dlg.SelectedPath}");
    }
}
