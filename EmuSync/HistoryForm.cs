using EmuSync.Core;

namespace EmuSync;

/// <summary>
/// The sync history, read from the shared log in Firestore: every run of every
/// device, and the files each one touched. Useful both as reassurance ("did the
/// laptop really upload that save?") and as the first place to look when a save
/// turns up older than expected.
///
/// The layout lives in HistoryForm.Designer.cs; this file only holds the behaviour.
/// </summary>
public partial class HistoryForm : Form
{
    private readonly EmuSyncServices _services;

    private List<SyncRunLog> _allRuns = new();

    /// <summary>Parameterless constructor required by the Visual Studio designer.</summary>
    private HistoryForm()
    {
        InitializeComponent();
        _services = null!;
    }

    public HistoryForm(EmuSyncServices services)
    {
        InitializeComponent();
        _services = services;

        _refresh.Click += async (_, _) => await LoadAsync();
        _filter.SelectedIndexChanged += (_, _) => ShowRuns();
        _runs.SelectedIndexChanged += (_, _) => ShowOperations();

        Shown += async (_, _) =>
        {
            if (_split.Height > 160) _split.SplitterDistance = (int)(_split.Height * 0.55);
            await LoadAsync();
        };
    }

    private async Task LoadAsync()
    {
        _refresh.Enabled = false;
        _status.Text = "Loading...";
        UseWaitCursor = true;
        try
        {
            _allRuns = await _services.LoadHistoryAsync();
            BuildFilter();
            ShowRuns();
            _status.Text = _allRuns.Count == 0
                ? "No sync recorded yet."
                : $"{_allRuns.Count} syncs · history is kept for one year";
        }
        catch (Exception ex)
        {
            _status.Text = "Error: " + ex.Message;
        }
        finally
        {
            UseWaitCursor = false;
            _refresh.Enabled = true;
        }
    }

    private void BuildFilter()
    {
        string? previous = _filter.SelectedItem as string;

        _filter.Items.Clear();
        _filter.Items.Add("All");
        foreach (string name in _allRuns.Select(r => r.EmulatorName).Distinct().OrderBy(n => n))
            _filter.Items.Add(name);

        int index = previous == null ? 0 : _filter.Items.IndexOf(previous);
        _filter.SelectedIndex = index < 0 ? 0 : index;
    }

    private IEnumerable<SyncRunLog> FilteredRuns()
    {
        string? selected = _filter.SelectedIndex > 0 ? _filter.SelectedItem as string : null;
        return selected == null ? _allRuns : _allRuns.Where(r => r.EmulatorName == selected);
    }

    private void ShowRuns()
    {
        _runs.BeginUpdate();
        _runs.Items.Clear();

        foreach (var run in FilteredRuns())
        {
            var item = new ListViewItem(new[]
            {
                run.StartedUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                run.EmulatorName,
                run.DeviceName,
                run.Error ?? run.Summary,
                run.Bytes > 0 ? FormatSize(run.Bytes) : "—",
                run.DurationMs >= 1000 ? $"{run.DurationMs / 1000.0:0.0} s" : $"{run.DurationMs} ms"
            })
            { Tag = run };

            if (run.Error != null) item.ForeColor = Color.Firebrick;
            else if (run.Conflicts > 0) item.ForeColor = Color.DarkOrange;

            _runs.Items.Add(item);
        }

        _runs.EndUpdate();
        ShowOperations();
    }

    private void ShowOperations()
    {
        _operations.BeginUpdate();
        _operations.Items.Clear();

        if (_runs.SelectedItems.Count == 0)
        {
            _detailHeader.Text = "Select a sync to see the files it touched.";
            _operations.EndUpdate();
            return;
        }

        var run = (SyncRunLog)_runs.SelectedItems[0].Tag!;

        _detailHeader.Text = run.Error != null
            ? $"{run.EmulatorName} on {run.DeviceName} — failed: {run.Error}"
            : $"{run.EmulatorName} ({run.Console}) on {run.DeviceName} — {run.Operations.Count} file(s)" +
              (run.Truncated ? ", list truncated" : "");

        foreach (var operation in run.Operations)
        {
            _operations.Items.Add(new ListViewItem(new[]
            {
                operation.ActionLabel,
                operation.Path,
                FormatSize(operation.Size),
                operation.AtUtc.ToLocalTime().ToString("HH:mm:ss")
            }));
        }

        _operations.EndUpdate();
    }

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1024 * 1024) return $"{bytes / 1024.0 / 1024.0:0.0} MB";
        if (bytes >= 1024) return $"{bytes / 1024.0:0.0} KB";
        return $"{bytes} B";
    }
}
