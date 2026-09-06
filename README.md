# EmuSync

*Leggilo in italiano: [README.it.md](README.it.md)*

Sync your emulator saves (memory cards, save files) with Google Drive.

For each configured folder, EmuSync compares local files with those on Drive
(`EmuSync/<ProfileName>` folder): if the content is identical (MD5 hash) it does
nothing, otherwise the file with the most recent modification time wins, in
both directions.

## Download

Grab the latest `EmuSync.exe` from the
[Releases page](https://github.com/Weskerw/EmuSync/releases/latest).

It is a single self-contained executable: no installer and no .NET runtime
required. Just run it and sign in with Google.

> Windows SmartScreen may show "Windows protected your PC" because the
> executable is not code-signed. Click **More info → Run anyway**.

## Features

- Multiple profiles: one folder per emulator (PCSX2, Dolphin, ...), subfolders included
- Automatic sync on startup
- Automatic sync when saves change (event-driven `FileSystemWatcher` + 30 s
  quiet period, virtually zero CPU/disk cost)
- Periodic Drive check (default every 15 min) to pick up changes made on other PCs
- "Last sync" date per profile
- Sign in with Google once; the token is stored locally and never asked again
- Change Google account from the Settings menu
- Guided first-run setup (sign in with Google, pick your save folders)
- Optional "Start with Windows" (Settings menu): starts hidden in the system
  tray, with quick actions (open, sync all, exit) on the tray icon

## Project structure

- **EmuSync.Core** — cross-platform .NET 8 library with all the logic
  (Google Drive client, comparison, sync engine). Reusable for a future
  Linux/Android version (e.g. with Avalonia or MAUI).
- **EmuSync** — WinForms GUI for Windows.

## Requirements

- Visual Studio 2022 with the **.NET desktop development** workload (.NET 8)
- A Google account

## Setting up Google credentials (developer only, one time, free)

**End users don't need any of this**: on first launch the browser opens with
"Sign in with Google" and that's it. The credentials below identify the
*application* and are created once by whoever builds/distributes it.

The app uses the official Google Drive API with the `drive.file` scope
(it can only see files it created itself, not your whole Drive).

1. Go to <https://console.cloud.google.com/> and sign in with your Google account.
2. Create a new project (any name, e.g. `EmuSync`).
3. Menu **APIs & Services → Library**: search for **Google Drive API** and press **Enable**.
4. Menu **APIs & Services → OAuth consent screen** (in newer consoles: **Google Auth Platform → Branding**):
   - user type **External**, fill in only the required fields (app name, your email);
   - under **Test users** add your Gmail address (the app can stay in testing mode).
5. Menu **APIs & Services → Credentials → Create credentials → OAuth client ID**:
   - application type: **Desktop app**;
   - download the JSON file.
6. Open the downloaded JSON and copy `client_id` and `client_secret` into the two
   constants in `EmuSync.Core\BuiltInCredentials.cs`: from that moment the app is
   self-contained and anyone using it only has to sign in with Google.
   (Alternatively you can put the file, renamed `credentials.json`, in the
   `EmuSync\` folder next to the `.csproj`: if present it takes precedence.)

On the first sync the browser opens to authorize the app; the token is saved
in `%APPDATA%\EmuSync\token` and you won't be asked again.

**Distributing the app to others**: as long as the OAuth consent screen is in
*Testing* mode, only accounts listed as test users can sign in. To open it to
everyone, press **Publish app** in the consent screen settings. With the
`drive.file` scope alone Google's full verification is not required, but users
may see an "unverified app" warning they can bypass via
*Advanced → Go to EmuSync*.

## Usage

1. Open `EmuSync.sln` in Visual Studio and press **F5**.
2. **Add folder...** → choose the emulator's saves folder
   (e.g. PCSX2's `memcards`, Dolphin's `GC`/`Wii`, etc.) and give it a name.
3. Add as many folders as you want, one per emulator.
4. Everything else is automatic: sync on startup, when saves change, and
   periodically to pick up remote changes. The **Sync** menu also has manual
   **Sync selected** (F5) and **Sync all** (Ctrl+F5). The log shows every operation.

On Drive, files end up in `My Drive/EmuSync/<ProfileName>/...`,
subfolders included.

## How it decides what to sync

For each file (union of local and remote):

- present only locally → uploaded;
- present only on Drive → downloaded;
- present on both sides → if the MD5 matches nothing happens; otherwise the
  most recent modification time wins (3 s tolerance for FAT filesystems).
  If the times match but the content differs, the conflict is logged and the
  file is skipped.

Modification times are preserved in both directions, so the comparison stays
reliable across multiple PCs.

## Contributing (public repo)

The OAuth credentials are **not in the repository**: `BuiltInCredentials.cs`
is committed with empty strings and `credentials.json` is in `.gitignore`.

- **To develop/test**: create your own test credentials in the Google Cloud
  Console (see above, ~5 minutes) and put the downloaded JSON, renamed
  `credentials.json`, in the `EmuSync\` folder next to the `.csproj`.
  The code picks it up automatically and git ignores it.
- **Official releases**: whoever publishes fills in the values in
  `BuiltInCredentials.cs` locally, builds, and does not commit the change.
- Never open pull requests containing real `client_id`/`client_secret` values.
  If they get committed by mistake, immediately regenerate the client secret
  in the Cloud Console (Credentials → your client ID → Reset secret).

## Notes and limitations

- Close the emulator before syncing manually (save files may be open for
  writing); the automatic sync already waits for a 30 s quiet period.
- Removing a profile does not touch any files, local or on Drive.
- Files deleted locally are re-downloaded from Drive (deletions are not
  propagated: the safest choice for save files).
- Future Linux desktop support only needs a new UI (Avalonia/GTK) on top of
  `EmuSync.Core`; Android will also need a dedicated OAuth flow.

## License

MIT — see [LICENSE](LICENSE).
