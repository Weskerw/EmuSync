# EmuSync

*Leggilo in italiano: [README.it.md](README.it.md)*

Keep your emulator saves (memory cards, save files) in sync across your
computers.

EmuSync uses two services, each for what it is good at:

- **Firebase** holds your *account* and your *configuration*: which emulators you
  sync, their labels, your settings and the index of the synced files.
- **Google Drive** holds the *save files themselves*, in **your own** Drive,
  under `My Drive/EmuSync/<emulator>/`. Your saves never pass through anybody
  else's storage and never count against anybody else's quota.

Saves are always grouped **by emulator**: PCSX2 goes to `EmuSync/pcsx2` on every
machine, even if the local folder is called something different on each one. Sign
in on a second PC and EmuSync already knows which emulators you sync — it just
looks for their folders locally (and offers to find them for you).

## Download

Grab the latest `EmuSync.exe` from the
[Releases page](https://github.com/Weskerw/EmuSync/releases/latest).

It is a single self-contained executable: no installer and no .NET runtime
required.

> Windows SmartScreen may show "Windows protected your PC" because the
> executable is not code-signed. Click **More info → Run anyway**.

## Features

- **EmuSync account** (Firebase Authentication): sign in with Google or with
  email + password. Signing in with Google authorizes Drive at the same time, so
  there is a single browser consent.
- **Emulator catalog with autodetect**: PCSX2, DuckStation, RPCS3, PPSSPP,
  Dolphin, Cemu, Ryujinx, yuzu, Citra, melonDS, mGBA, RetroArch, Flycast, Xenia,
  Project64, Snes9x — plus a "custom" option for anything else.
- **Configuration shared across devices**: the emulator list lives in your
  account, the local paths stay per-device.
- Automatic sync on startup, when saves change (event-driven `FileSystemWatcher`
  + 30 s quiet period, virtually zero CPU/disk cost) and periodically (default
  every 15 min) to pick up changes from other PCs.
- **Deletions propagate safely**: a file index in Firestore tells a real deletion
  apart from a file that was simply never downloaded. Removed files go to the
  Drive trash and to a local `%APPDATA%\EmuSync\trash` folder, never straight to
  oblivion. If the local folder is empty, deletions are never propagated.
- Guided first-run setup, "Start with Windows" (hidden in the tray, with quick
  actions on the tray icon), per-emulator "last sync" time.

## Project structure

- **EmuSync.Core** — cross-platform .NET 8 library with all the logic: Firebase
  Auth and Firestore over REST (`HttpClient` only), the emulator catalog, the
  Drive client and the sync engine.
- **EmuSync** — WinForms GUI for Windows.
- **site/** — the public website and the Firestore security rules.

The only platform-specific piece is `IGoogleAuthorizationProvider`, the OAuth
consent flow. `DesktopGoogleAuthProvider` implements it for Windows/Linux/macOS
(system browser + loopback listener); an Android build would add its own
(Custom Tabs / AppAuth) and reuse everything else unchanged.

## Requirements

- Visual Studio 2022 with the **.NET desktop development** workload (.NET 8)
- A Google account

## Setting up Firebase (developer only, one time, free)

**End users don't need any of this.** These steps identify the *application* and
are done once by whoever builds and distributes it.

Everything below fits comfortably in the free **Spark** plan: Firebase only
stores small JSON documents (no files — those go to Drive).

1. Go to <https://console.firebase.google.com/> and create a project (or open the
   existing `emusync-43d2b`). **Use the same Google Cloud project as the OAuth
   credentials below**: Firebase only accepts Google ID tokens issued to one of
   its own project's OAuth clients.
2. **Build → Authentication → Get started**, then enable both sign-in providers:
   - **Email/Password**
   - **Google** (set the support email; the project's OAuth clients are trusted
     automatically)
3. **Build → Firestore Database → Create database**, in production mode, in the
   region closest to you.
4. Deploy the security rules (they scope every document to its owner):
   ```
   cd site
   firebase deploy --only firestore:rules
   ```
5. **Project settings → General → Your apps → Web app**: copy `apiKey` and
   `projectId`, then either
   - put them in `EmuSync.Core\FirebaseOptions.cs`
     (`EmbeddedApiKey` / `EmbeddedProjectId`), or
   - create `EmuSync\emusync-firebase.json` (git-ignored, takes precedence):
     ```json
     { "apiKey": "AIza...", "projectId": "emusync-43d2b" }
     ```

## Setting up Google Drive credentials (developer only, one time, free)

The app uses the official Google Drive API with the `drive.file` scope (it can
only see files it created itself, not your whole Drive).

1. In <https://console.cloud.google.com/>, select the **same project** as
   Firebase.
2. **APIs & Services → Library**: enable **Google Drive API**.
3. **APIs & Services → OAuth consent screen** (newer consoles: **Google Auth
   Platform → Branding**): user type **External**, fill in the required fields
   and add your address under **Test users** while the app is in testing mode.
4. **APIs & Services → Credentials → Create credentials → OAuth client ID**,
   application type **Desktop app**, then download the JSON.
5. Copy `client_id` and `client_secret` into `EmuSync.Core\BuiltInCredentials.cs`,
   or drop the file, renamed `credentials.json`, into the `EmuSync\` folder next
   to the `.csproj` (git-ignored, takes precedence).

The Drive consent requests the `openid`, `email` and `profile` scopes as well:
that is what produces the Google ID token EmuSync exchanges for a Firebase
session, so "Continue with Google" covers both services at once.

Tokens are stored in `%APPDATA%\EmuSync\` — the Drive refresh token under
`token\`, the Firebase one in `session.json`, encrypted with DPAPI for the
current Windows user.

**Distributing the app**: while the OAuth consent screen is in *Testing* mode,
only accounts listed as test users can sign in with Google. Press **Publish app**
to open it to everyone. With the `drive.file` scope alone Google's full
verification is not required, but users may see an "unverified app" warning they
can bypass via *Advanced → Go to EmuSync*.

## Usage

1. Open `EmuSync.sln` in Visual Studio and press **F5**.
2. First run: sign in (Google or email), authorize Drive, then tick the emulators
   EmuSync found on the PC.
3. Later: **Sync → Add emulator...** to add one by hand, **Settings → Detect
   emulators on this PC...** to rescan, **Sync → Set local folder...** to point an
   emulator configured on another device at a folder here.
4. Everything else is automatic. The **Sync** menu still has manual **Sync
   selected** (F5) and **Sync all** (Ctrl+F5); the log shows every operation.

## How it decides what to sync

For each file EmuSync compares three states: the local copy, the copy on Drive
and the **index** of what was last synced (stored in Firestore).

- new locally → uploaded; new on Drive → downloaded;
- identical content (MD5) → nothing to do;
- changed on one side only → that side wins, no guessing needed;
- changed on both sides → the most recent modification time wins (3 s tolerance
  for FAT filesystems); if the times match too, the conflict is logged and both
  copies are left alone;
- in the index but gone from one side → it was deleted there, so the deletion is
  propagated (to the Drive trash, or to `%APPDATA%\EmuSync\trash` locally). If
  the file also changed on the other side, it is resurrected instead of deleted.

Modification times are preserved in both directions, so the comparison stays
reliable across multiple PCs.

## Data stored in Firestore

```
users/{uid}                     email, settings, list of emulators
users/{uid}/devices/{deviceId}  device name and its local folders
users/{uid}/emulators/{key}     file index (path, MD5, size, date, Drive id)
```

No save data is stored in Firebase — only metadata. The security rules make each
tree readable and writable by its owner alone.

## Contributing (public repo)

Credentials are **not in the repository**: `BuiltInCredentials.cs` is committed
with empty strings, and `credentials.json` and `emusync-firebase.json` are in
`.gitignore`.

- **To develop/test**: create your own Firebase project and your own OAuth
  credentials (see above) and keep them in the two git-ignored files.
- **Official releases**: whoever publishes fills in the values locally, builds,
  and does not commit the change.
- Never open pull requests containing real `client_id`/`client_secret` values.
  If they get committed by mistake, immediately regenerate the client secret in
  the Cloud Console (Credentials → your client ID → Reset secret).
  The Firebase `apiKey` is not a secret (every web app ships it) — the security
  rules are what protects the data.

## Notes and limitations

- Close the emulator before syncing manually (save files may be open for
  writing); the automatic sync already waits for a 30 s quiet period.
- Removing an emulator never touches any file, local or on Drive.
- Signing out leaves your configuration in the cloud: sign back in and it
  returns.
- Linux/macOS desktop support only needs a new UI (Avalonia/GTK) on top of
  `EmuSync.Core`; Android additionally needs its own
  `IGoogleAuthorizationProvider` and scoped-storage handling.

## License

MIT — see [LICENSE](LICENSE).
