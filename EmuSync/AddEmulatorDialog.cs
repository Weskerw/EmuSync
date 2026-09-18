using EmuSync.Core;

namespace EmuSync;

/// <summary>
/// Picks an emulator from the catalog and the folder holding its saves on this
/// machine. The emulator choice decides the remote folder (EmuSync/&lt;key&gt;),
/// which is what keeps the same console together across devices.
/// </summary>
public class AddEmulatorDialog : Form
{
    private const string OtherLabel = "Other (custom)...";

    private readonly ComboBox _emulator = new();
    private readonly Label _lblCustom = new();
    private readonly TextBox _customName = new();
    private readonly Label _lblFolder = new();
    private readonly TextBox _folder = new();
    private readonly Button _browse = new();
    private readonly Label _hint = new();
    private readonly Button _ok = new();
    private readonly Button _cancel = new();

    private readonly HashSet<string> _alreadyUsed;

    /// <summary>The chosen emulator (a custom entry when "Other" was selected).</summary>
    public EmulatorInfo? SelectedEmulator { get; private set; }

    /// <summary>The chosen local folder.</summary>
    public string SelectedPath => _folder.Text.Trim();

    /// <param name="alreadyUsed">Keys already enrolled, hidden from the list.</param>
    /// <param name="lockedTo">When set, the emulator is fixed and only the folder can be chosen.</param>
    public AddEmulatorDialog(IEnumerable<string> alreadyUsed, EmulatorInfo? lockedTo = null)
    {
        _alreadyUsed = new HashSet<string>(alreadyUsed, StringComparer.OrdinalIgnoreCase);

        Text = lockedTo == null ? "Add an emulator" : $"Save folder – {lockedTo.DisplayName}";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(520, 260);

        var lblEmulator = new Label { Text = "Emulator / console", AutoSize = true };
        lblEmulator.SetBounds(20, 18, 200, 20);

        _emulator.SetBounds(20, 40, 480, 24);
        _emulator.DropDownStyle = ComboBoxStyle.DropDownList;
        _emulator.SelectedIndexChanged += (_, _) => OnEmulatorChanged();

        _lblCustom.Text = "Name";
        _lblCustom.SetBounds(20, 74, 100, 20);
        _customName.SetBounds(20, 94, 480, 24);

        _lblFolder.Text = "Local save folder";
        _lblFolder.SetBounds(20, 128, 200, 20);
        _folder.SetBounds(20, 148, 390, 24);
        _browse.Text = "Browse...";
        _browse.SetBounds(418, 147, 82, 26);
        _browse.Click += (_, _) => Browse();

        _hint.SetBounds(20, 178, 480, 36);
        _hint.ForeColor = SystemColors.GrayText;

        _ok.Text = "OK";
        _ok.SetBounds(334, 220, 80, 28);
        _ok.Click += (_, _) => Confirm();
        _cancel.Text = "Cancel";
        _cancel.SetBounds(420, 220, 80, 28);
        _cancel.DialogResult = DialogResult.Cancel;

        Controls.AddRange(new Control[]
        {
            lblEmulator, _emulator, _lblCustom, _customName, _lblFolder, _folder, _browse, _hint, _ok, _cancel
        });
        AcceptButton = _ok;
        CancelButton = _cancel;

        if (lockedTo != null)
        {
            _emulator.Items.Add(lockedTo);
            _emulator.SelectedIndex = 0;
            _emulator.Enabled = false;
        }
        else
        {
            foreach (var emulator in EmulatorCatalog.All.Where(e => !_alreadyUsed.Contains(e.Key)))
                _emulator.Items.Add(emulator);
            _emulator.Items.Add(OtherLabel);
            _emulator.SelectedIndex = 0;
        }

        OnEmulatorChanged();
    }

    private EmulatorInfo? CatalogSelection => _emulator.SelectedItem as EmulatorInfo;

    private void OnEmulatorChanged()
    {
        bool custom = CatalogSelection == null;
        _lblCustom.Visible = _customName.Visible = custom;
        _hint.Text = CatalogSelection?.Hint ?? "The name becomes the folder used on Drive (e.g. \"vita3k\").";

        // Prefill the folder with the first candidate that exists on this machine.
        if (CatalogSelection is { } info && _folder.Text.Length == 0)
        {
            foreach (string template in info.CandidatePaths)
            {
                string path = EmulatorCatalog.ExpandPath(template);
                if (!Directory.Exists(path)) continue;
                _folder.Text = path;
                break;
            }
        }
    }

    /// <summary>Preselects a folder (used when the wizard already detected one).</summary>
    public void SetFolder(string path) => _folder.Text = path;

    private void Browse()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Choose the emulator's saves / memory card folder",
            SelectedPath = Directory.Exists(_folder.Text) ? _folder.Text : ""
        };
        if (dlg.ShowDialog(this) == DialogResult.OK) _folder.Text = dlg.SelectedPath;
    }

    private void Confirm()
    {
        var info = CatalogSelection;

        if (info == null)
        {
            string name = _customName.Text.Trim();
            if (name.Length == 0)
            {
                MessageBox.Show(this, "Enter a name for the emulator.", "EmuSync",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string key = EmulatorCatalog.MakeKey(name);
            if (_alreadyUsed.Contains(key))
            {
                MessageBox.Show(this, "This emulator is already in the list.", "EmuSync",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            info = new EmulatorInfo
            {
                Key = key,
                DisplayName = name,
                Console = "Custom",
                IsCustom = true
            };
        }

        if (!Directory.Exists(SelectedPath))
        {
            MessageBox.Show(this, "Choose an existing folder.", "EmuSync",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SelectedEmulator = info;
        DialogResult = DialogResult.OK;
        Close();
    }
}
