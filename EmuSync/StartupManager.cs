using Microsoft.Win32;

namespace EmuSync;

/// <summary>"Start with Windows" via the per-user Run registry key (no admin rights needed).</summary>
public static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "EmuSync";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(RunValueName) != null;
        }
        catch
        {
            return false;
        }
    }

    public static void SetEnabled(bool enable)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath)
            ?? throw new InvalidOperationException("Cannot open the Windows startup registry key.");
        if (enable)
            key.SetValue(RunValueName, $"\"{Application.ExecutablePath}\" --minimized");
        else
            key.DeleteValue(RunValueName, false);
    }
}
