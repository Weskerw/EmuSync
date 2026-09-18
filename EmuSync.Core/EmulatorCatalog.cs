using System.Text;

namespace EmuSync.Core;

/// <summary>
/// One known emulator: its remote folder name, the console it emulates and where
/// it usually keeps its saves.
/// </summary>
public sealed class EmulatorInfo
{
    /// <summary>
    /// Stable lowercase id, also used verbatim as the folder name on Drive
    /// (EmuSync/pcsx2/...) and as the Firestore document id. Never change it for
    /// an existing emulator: it would orphan everyone's saves.
    /// </summary>
    public string Key { get; init; } = "";

    public string DisplayName { get; init; } = "";

    /// <summary>The console being emulated, shown in the UI ("PlayStation 2").</summary>
    public string Console { get; init; } = "";

    /// <summary>
    /// Candidate save folders, most likely first. Templates may contain
    /// environment variables (%APPDATA%) and the {Documents} placeholder.
    /// </summary>
    public string[] CandidatePaths { get; init; } = Array.Empty<string>();

    /// <summary>Shown when autodetect fails, to help the user pick the right folder.</summary>
    public string Hint { get; init; } = "";

    /// <summary>True for entries the user created by hand ("Other...").</summary>
    public bool IsCustom { get; init; }

    public override string ToString() => $"{DisplayName} ({Console})";
}

/// <summary>An emulator that was actually found on this machine.</summary>
public sealed record DetectedEmulator(EmulatorInfo Emulator, string LocalPath);

/// <summary>
/// The list of emulators EmuSync knows about, plus detection of their save
/// folders on this machine.
///
/// The key point of the new layout: saves are always grouped by emulator, so
/// PCSX2 memory cards land in EmuSync/pcsx2 on every device, whatever the local
/// folder happens to be called. That is what makes a save written on the desktop
/// show up on the laptop — and, one day, on Android.
/// </summary>
public static class EmulatorCatalog
{
    public static IReadOnlyList<EmulatorInfo> All { get; } = new[]
    {
        new EmulatorInfo
        {
            Key = "pcsx2",
            DisplayName = "PCSX2",
            Console = "PlayStation 2",
            CandidatePaths = new[] { @"{Documents}\PCSX2\memcards", @"%APPDATA%\PCSX2\memcards" },
            Hint = "Folder 'memcards' inside Documents\\PCSX2."
        },
        new EmulatorInfo
        {
            Key = "duckstation",
            DisplayName = "DuckStation",
            Console = "PlayStation 1",
            CandidatePaths = new[] { @"{Documents}\DuckStation\memcards", @"%APPDATA%\DuckStation\memcards" },
            Hint = "Folder 'memcards' inside Documents\\DuckStation."
        },
        new EmulatorInfo
        {
            Key = "rpcs3",
            DisplayName = "RPCS3",
            Console = "PlayStation 3",
            CandidatePaths = new[]
            {
                @"{Documents}\RPCS3\dev_hdd0\home\00000001\savedata",
                @"%LOCALAPPDATA%\rpcs3\dev_hdd0\home\00000001\savedata"
            },
            Hint = "RPCS3 is portable: look for dev_hdd0\\home\\00000001\\savedata next to rpcs3.exe."
        },
        new EmulatorInfo
        {
            Key = "ppsspp",
            DisplayName = "PPSSPP",
            Console = "PSP",
            CandidatePaths = new[]
            {
                @"{Documents}\PPSSPP\PSP\SAVEDATA",
                @"%USERPROFILE%\Documents\PPSSPP\PSP\SAVEDATA"
            },
            Hint = "Folder PSP\\SAVEDATA of the memstick used by PPSSPP."
        },
        new EmulatorInfo
        {
            Key = "dolphin",
            DisplayName = "Dolphin",
            Console = "GameCube / Wii",
            CandidatePaths = new[]
            {
                @"{Documents}\Dolphin Emulator\GC",
                @"%APPDATA%\Dolphin Emulator\GC"
            },
            Hint = "Folder 'GC' (GameCube memory cards) or 'Wii' inside Dolphin Emulator."
        },
        new EmulatorInfo
        {
            Key = "cemu",
            DisplayName = "Cemu",
            Console = "Wii U",
            CandidatePaths = new[]
            {
                @"%APPDATA%\Cemu\mlc01\usr\save",
                @"{Documents}\Cemu\mlc01\usr\save"
            },
            Hint = "Folder mlc01\\usr\\save (next to Cemu.exe on portable installs)."
        },
        new EmulatorInfo
        {
            Key = "ryujinx",
            DisplayName = "Ryujinx",
            Console = "Nintendo Switch",
            CandidatePaths = new[] { @"%APPDATA%\Ryujinx\bis\user\save" },
            Hint = "Folder bis\\user\\save in the Ryujinx data directory."
        },
        new EmulatorInfo
        {
            Key = "yuzu",
            DisplayName = "yuzu",
            Console = "Nintendo Switch",
            CandidatePaths = new[] { @"%APPDATA%\yuzu\nand\user\save" },
            Hint = "Folder nand\\user\\save in the yuzu data directory."
        },
        new EmulatorInfo
        {
            Key = "citra",
            DisplayName = "Citra",
            Console = "Nintendo 3DS",
            CandidatePaths = new[] { @"%APPDATA%\Citra\sdmc\Nintendo 3DS", @"%APPDATA%\Citra\sdmc" },
            Hint = "Folder 'sdmc' in the Citra data directory."
        },
        new EmulatorInfo
        {
            Key = "melonds",
            DisplayName = "melonDS",
            Console = "Nintendo DS",
            CandidatePaths = new[] { @"%APPDATA%\melonDS\savefiles", @"%LOCALAPPDATA%\melonDS\savefiles" },
            Hint = "By default melonDS saves next to the ROMs: pick that folder."
        },
        new EmulatorInfo
        {
            Key = "mgba",
            DisplayName = "mGBA",
            Console = "Game Boy / Advance",
            CandidatePaths = new[] { @"{Documents}\mGBA\saves", @"%APPDATA%\mGBA\saves" },
            Hint = "By default mGBA saves next to the ROMs: pick that folder."
        },
        new EmulatorInfo
        {
            Key = "retroarch",
            DisplayName = "RetroArch",
            Console = "Multi-system",
            CandidatePaths = new[] { @"%APPDATA%\RetroArch\saves", @"%USERPROFILE%\RetroArch\saves" },
            Hint = "Folder 'saves' in the RetroArch directory (portable installs keep it next to retroarch.exe)."
        },
        new EmulatorInfo
        {
            Key = "flycast",
            DisplayName = "Flycast",
            Console = "Dreamcast",
            CandidatePaths = new[] { @"%APPDATA%\flycast\data", @"%USERPROFILE%\flycast\data" },
            Hint = "Folder 'data' of Flycast (holds the VMU files)."
        },
        new EmulatorInfo
        {
            Key = "xenia",
            DisplayName = "Xenia",
            Console = "Xbox 360",
            CandidatePaths = new[] { @"{Documents}\Xenia\content", @"%LOCALAPPDATA%\Xenia\content" },
            Hint = "Folder 'content' next to xenia.exe."
        },
        new EmulatorInfo
        {
            Key = "project64",
            DisplayName = "Project64",
            Console = "Nintendo 64",
            CandidatePaths = new[] { @"%APPDATA%\Project64\Save", @"{Documents}\Project64\Save" },
            Hint = "Folder 'Save' in the Project64 directory."
        },
        new EmulatorInfo
        {
            Key = "snes9x",
            DisplayName = "Snes9x",
            Console = "Super Nintendo",
            CandidatePaths = new[] { @"%APPDATA%\Snes9x\Saves", @"{Documents}\Snes9x\Saves" },
            Hint = "Folder 'Saves' in the Snes9x directory."
        }
    };

    public static EmulatorInfo? Find(string key) =>
        All.FirstOrDefault(e => string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Returns the catalog entry for a key, inventing a "custom" one when the key
    /// is not in the catalog (profiles created with "Other..." or imported from an
    /// older config).
    /// </summary>
    public static EmulatorInfo Resolve(string key) =>
        Find(key) ?? new EmulatorInfo
        {
            Key = key,
            DisplayName = key,
            Console = "Custom",
            IsCustom = true
        };

    /// <summary>
    /// Turns a free-form name into a key usable as a folder name and a Firestore
    /// document id: lowercase, ASCII letters/digits/dash only.
    /// </summary>
    public static string MakeKey(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (char c in name.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c) && c < 128) sb.Append(c);
            else if (c is ' ' or '-' or '_' or '.') { if (sb.Length > 0 && sb[^1] != '-') sb.Append('-'); }
        }
        string key = sb.ToString().Trim('-');
        return key.Length == 0 ? "custom" : key;
    }

    /// <summary>Expands %ENV% variables and the {Documents} placeholder.</summary>
    public static string ExpandPath(string template)
    {
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Environment.ExpandEnvironmentVariables(template.Replace("{Documents}", documents));
    }

    /// <summary>
    /// Scans the machine for the save folders of the known emulators. Only
    /// folders that exist and contain at least one file are reported, so an empty
    /// leftover directory does not create a useless profile.
    /// </summary>
    public static List<DetectedEmulator> Detect()
    {
        var found = new List<DetectedEmulator>();

        foreach (var emulator in All)
        {
            foreach (string template in emulator.CandidatePaths)
            {
                string path;
                try { path = ExpandPath(template); }
                catch { continue; }

                if (!Directory.Exists(path)) continue;
                if (!HasAnyFile(path)) continue;

                found.Add(new DetectedEmulator(emulator, path));
                break; // first match wins
            }
        }

        return found;
    }

    private static bool HasAnyFile(string path)
    {
        try
        {
            return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Any();
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }
}
