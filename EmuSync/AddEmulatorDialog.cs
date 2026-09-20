using EmuSync.Core;

namespace EmuSync;

/// <summary>
/// Picks an emulator from the catalog and the folder holding its saves on this
/// machine. The emulator choice decides the remote folder (EmuSync/&lt;key&gt;),
/// which is what keeps the same console together across devices.
///
/// The layout lives in AddEmulatorDialog.Designer.cs; this file only holds the
/// behaviour.
/// </summary>
public partial class AddEmulatorDialog : Form
{
    private const string OtherLabel = "Other (custom)...";

    private readonly HashSet<string> _alreadyUsed;

    /// <summary>The chosen emulator (a custom entry when "Other" was selected).</summary>
    public EmulatorInfo? SelectedEmulator { get; private set; }

    /// <summary>The chosen local folder.</summary>
    public string SelectedPath => _folder.Text.Trim();

    /// <summary>Parameterless constructor required by the Visual Studio designer.</summary>
    private AddEmulatorDialog()
    {
        InitializeComponent();
        _alreadyUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <param name="alreadyUsed">Keys already enrolled, hidden from the list.</param>
    /// <param name="lockedTo">When set, the emulator is fixed and only the folder can be chosen.</param>
    public AddEmulatorDialog(IEnumerable<string> alreadyUsed, EmulatorInfo? lockedTo = null)
    {
        InitializeComponent();

        _alreadyUsed = new HashSet<string>(alreadyUsed, StringComparer.OrdinalIgnoreCase);

        if (lockedTo != null) Text = $"Save folder – {lockedTo.DisplayName}";

        _emulator.SelectedIndexChanged += (_, _) => OnEmulatorChanged();
        _browse.Click += (_, _) => Browse();
        _ok.Click += (_, _) => Confirm();

        AcceptButton = _ok;

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
