namespace EmuSync;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        // --minimized is used by the "Start with Windows" entry: start hidden in the tray.
        Application.Run(new MainForm(args.Contains("--minimized")));
    }
}
