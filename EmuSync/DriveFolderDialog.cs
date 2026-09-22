using EmuSync.Core;

namespace EmuSync;

/// <summary>
/// Chooses the Drive folder that holds the saves.
///
/// It is typed rather than picked from a tree on purpose: EmuSync asks for the
/// <c>drive.file</c> permission, which lets it see only what it created itself,
/// so there is nothing to browse. Every folder in the path is created by the app.
///
/// The layout lives in DriveFolderDialog.Designer.cs; this file only holds the
/// behaviour.
/// </summary>
public partial class DriveFolderDialog : Form
{
    private readonly string _currentPath;

    /// <summary>The chosen path, normalized.</summary>
    public string SelectedPath => DrivePath.Normalize(_path.Text);

    /// <summary>Parameterless constructor required by the Visual Studio designer.</summary>
    private DriveFolderDialog() : this(DrivePath.Default) { }

    public DriveFolderDialog(string currentPath)
    {
        InitializeComponent();

        _currentPath = DrivePath.Normalize(currentPath);
        _path.Text = _currentPath;

        _path.TextChanged += (_, _) => UpdatePreview();
        _ok.Click += (_, _) => Confirm();

        AcceptButton = _ok;
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        string? problem = DrivePath.Validate(_path.Text);
        string normalized = DrivePath.Normalize(_path.Text);

        _preview.Text = problem == null
            ? $"Saves will go to:  My Drive / {normalized.Replace("/", " / ")} / <emulator>"
            : "";

        bool changed = !string.Equals(normalized, _currentPath, StringComparison.OrdinalIgnoreCase);

        if (problem != null)
        {
            _warning.ForeColor = Color.Firebrick;
            _warning.Text = problem;
        }
        else if (changed)
        {
            _warning.ForeColor = SystemColors.ControlText;
            _warning.Text = $"The existing \"{_currentPath}\" folder will be moved and renamed, " +
                            "with everything inside it: nothing is re-uploaded and nothing is lost. " +
                            "Your other computers will follow at their next sync.";
        }
        else
        {
            _warning.Text = "";
        }

        _ok.Enabled = problem == null;
    }

    private void Confirm()
    {
        string? problem = DrivePath.Validate(_path.Text);
        if (problem != null)
        {
            MessageBox.Show(this, problem, "EmuSync", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
