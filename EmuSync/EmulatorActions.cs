using EmuSync.Core;

namespace EmuSync;

/// <summary>
/// The commands that add, remove and re-point emulators, in one place because
/// they are reachable from two: the toolbar of the main window and the Emulators
/// section of the settings window.
///
/// Each one shows its own dialogs and returns whether anything actually changed,
/// so the caller knows when to refresh its list.
/// </summary>
internal static class EmulatorActions
{
    public static async Task<bool> AddAsync(Form owner, EmuSyncServices services, Action<string> log)
    {
        using var dlg = new AddEmulatorDialog(services.Profiles.Select(p => p.Key));
        if (dlg.ShowDialog(owner) != DialogResult.OK || dlg.SelectedEmulator == null) return false;

        return await RunAsync(owner, log, async () =>
        {
            await services.AddEmulatorAsync(dlg.SelectedEmulator, dlg.SelectedPath);
            log($"Added {dlg.SelectedEmulator.DisplayName} → {dlg.SelectedPath} " +
                $"(Drive: {services.Config.DriveFolder}/{dlg.SelectedEmulator.Key})");
        });
    }

    public static async Task<bool> SetFolderAsync(Form owner, EmuSyncServices services, EmulatorProfile profile,
        Action<string> log)
    {
        using var dlg = new AddEmulatorDialog(Array.Empty<string>(), profile.Info);
        if (profile.IsLinkedHere) dlg.SetFolder(profile.LocalPath);
        if (dlg.ShowDialog(owner) != DialogResult.OK) return false;

        return await RunAsync(owner, log, async () =>
        {
            await services.SetLocalPathAsync(profile.Key, dlg.SelectedPath);
            log($"{profile.DisplayName}: local folder set to {dlg.SelectedPath}");
        });
    }

    public static async Task<bool> RemoveAsync(Form owner, EmuSyncServices services, EmulatorProfile profile,
        Action<string> log)
    {
        var answer = MessageBox.Show(owner,
            $"Stop syncing '{profile.DisplayName}'?\n\n" +
            "Yes  = remove it from every device\n" +
            "No   = only unlink it from this PC\n\n" +
            "(Files on disk and on Drive are NOT touched.)",
            "EmuSync", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        if (answer == DialogResult.Cancel) return false;

        return await RunAsync(owner, log, async () =>
        {
            await services.RemoveEmulatorAsync(profile.Key, everywhere: answer == DialogResult.Yes);
            log($"Removed '{profile.DisplayName}'.");
        });
    }

    public static async Task<bool> DetectAsync(Form owner, EmuSyncServices services, Action<string> log)
    {
        var detected = services.DetectNewEmulators();
        if (detected.Count == 0)
        {
            MessageBox.Show(owner, "No new emulator found on this PC.", "EmuSync",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        string list = string.Join("\n", detected.Select(d => $"• {d.Emulator.DisplayName} → {d.LocalPath}"));
        if (MessageBox.Show(owner, $"Found {detected.Count} emulator(s):\n\n{list}\n\nAdd them to sync?",
                "EmuSync", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
            return false;

        return await RunAsync(owner, log, async () =>
        {
            foreach (var item in detected)
                await services.AddEmulatorAsync(item.Emulator, item.LocalPath);
            log($"{detected.Count} emulator(s) added.");
        });
    }

    public static async Task<bool> ChangeDriveFolderAsync(Form owner, EmuSyncServices services, Action<string> log)
    {
        using var dlg = new DriveFolderDialog(services.Config.DriveFolder);
        if (dlg.ShowDialog(owner) != DialogResult.OK) return false;
        if (string.Equals(dlg.SelectedPath, services.Config.DriveFolder, StringComparison.OrdinalIgnoreCase))
            return false;

        return await RunAsync(owner, log, async () =>
        {
            log($"Moving the Drive folder to '{dlg.SelectedPath}'...");
            bool moved = await services.SetDriveFolderAsync(dlg.SelectedPath);
            log(moved
                ? $"Drive folder moved to '{dlg.SelectedPath}': the saves came along, nothing to re-upload."
                : $"Drive folder set to '{dlg.SelectedPath}' (there was nothing to move yet).");
        });
    }

    public static async Task<bool> ChangeDriveAccountAsync(Form owner, EmuSyncServices services, Action<string> log)
    {
        if (MessageBox.Show(owner,
                "Disconnect the current Google Drive account?\n" +
                "Your browser will open to choose another one. Your EmuSync account stays the same.",
                "EmuSync", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
            return false;

        return await RunAsync(owner, log, async () =>
        {
            await services.Drive.ReauthorizeAsync();
            log("Google Drive account changed.");
        });
    }

    /// <summary>Runs one command with the wait cursor on and errors reported once.</summary>
    private static async Task<bool> RunAsync(Form owner, Action<string> log, Func<Task> action)
    {
        owner.UseWaitCursor = true;
        try
        {
            await action();
            return true;
        }
        catch (Exception ex)
        {
            log("ERROR: " + ex.Message);
            MessageBox.Show(owner, ex.Message, "EmuSync", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
        finally
        {
            owner.UseWaitCursor = false;
        }
    }
}
