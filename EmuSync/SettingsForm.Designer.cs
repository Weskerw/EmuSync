namespace EmuSync;

partial class SettingsForm
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null!;

    private System.Windows.Forms.ListBox _sections;
    private System.Windows.Forms.Panel _content;
    private System.Windows.Forms.Panel _footer;
    private System.Windows.Forms.Button _close;

    // --- General ---
    private System.Windows.Forms.Panel _pageGeneral;
    private System.Windows.Forms.Label _titleGeneral;
    private System.Windows.Forms.CheckBox _chkAutoSync;
    private System.Windows.Forms.Label _descAutoSync;
    private System.Windows.Forms.CheckBox _chkRemoteCheck;
    private System.Windows.Forms.NumericUpDown _numRemoteMinutes;
    private System.Windows.Forms.Label _lblMinutes;
    private System.Windows.Forms.Label _descRemoteCheck;
    private System.Windows.Forms.CheckBox _chkStartup;
    private System.Windows.Forms.Label _descStartup;

    // --- Account ---
    private System.Windows.Forms.Panel _pageAccount;
    private System.Windows.Forms.Label _titleAccount;
    private System.Windows.Forms.Label _lblAccountCaption;
    private System.Windows.Forms.Label _lblAccountEmail;
    private System.Windows.Forms.Button _btnSignOut;
    private System.Windows.Forms.Label _lblDriveCaption;
    private System.Windows.Forms.Label _lblDriveState;
    private System.Windows.Forms.Button _btnChangeDrive;
    private System.Windows.Forms.Label _lblFolderCaption;
    private System.Windows.Forms.Label _lblDriveFolder;
    private System.Windows.Forms.Button _btnDriveFolder;

    // --- Emulators ---
    private System.Windows.Forms.Panel _pageEmulators;
    private System.Windows.Forms.Label _titleEmulators;
    private System.Windows.Forms.ListView _emulators;
    private System.Windows.Forms.ColumnHeader _colEmulator;
    private System.Windows.Forms.ColumnHeader _colConsole;
    private System.Windows.Forms.ColumnHeader _colFolder;
    private System.Windows.Forms.Button _btnAdd;
    private System.Windows.Forms.Button _btnSetFolder;
    private System.Windows.Forms.Button _btnRemove;
    private System.Windows.Forms.Button _btnDetect;

    // --- Info ---
    private System.Windows.Forms.Panel _pageInfo;
    private System.Windows.Forms.Label _titleInfo;
    private System.Windows.Forms.Label _lblVersion;
    private System.Windows.Forms.LinkLabel _linkSite;
    private System.Windows.Forms.LinkLabel _linkGitHub;
    private System.Windows.Forms.Label _lblPaths;
    private System.Windows.Forms.LinkLabel _linkConfigFolder;
    private System.Windows.Forms.Label _lblLicense;

    /// <summary>Clean up any resources being used.</summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this._sections = new System.Windows.Forms.ListBox();
        this._content = new System.Windows.Forms.Panel();
        this._footer = new System.Windows.Forms.Panel();
        this._close = new System.Windows.Forms.Button();
        this._pageGeneral = new System.Windows.Forms.Panel();
        this._titleGeneral = new System.Windows.Forms.Label();
        this._chkAutoSync = new System.Windows.Forms.CheckBox();
        this._descAutoSync = new System.Windows.Forms.Label();
        this._chkRemoteCheck = new System.Windows.Forms.CheckBox();
        this._numRemoteMinutes = new System.Windows.Forms.NumericUpDown();
        this._lblMinutes = new System.Windows.Forms.Label();
        this._descRemoteCheck = new System.Windows.Forms.Label();
        this._chkStartup = new System.Windows.Forms.CheckBox();
        this._descStartup = new System.Windows.Forms.Label();
        this._pageAccount = new System.Windows.Forms.Panel();
        this._titleAccount = new System.Windows.Forms.Label();
        this._lblAccountCaption = new System.Windows.Forms.Label();
        this._lblAccountEmail = new System.Windows.Forms.Label();
        this._btnSignOut = new System.Windows.Forms.Button();
        this._lblDriveCaption = new System.Windows.Forms.Label();
        this._lblDriveState = new System.Windows.Forms.Label();
        this._btnChangeDrive = new System.Windows.Forms.Button();
        this._lblFolderCaption = new System.Windows.Forms.Label();
        this._lblDriveFolder = new System.Windows.Forms.Label();
        this._btnDriveFolder = new System.Windows.Forms.Button();
        this._pageEmulators = new System.Windows.Forms.Panel();
        this._titleEmulators = new System.Windows.Forms.Label();
        this._emulators = new System.Windows.Forms.ListView();
        this._colEmulator = new System.Windows.Forms.ColumnHeader();
        this._colConsole = new System.Windows.Forms.ColumnHeader();
        this._colFolder = new System.Windows.Forms.ColumnHeader();
        this._btnAdd = new System.Windows.Forms.Button();
        this._btnSetFolder = new System.Windows.Forms.Button();
        this._btnRemove = new System.Windows.Forms.Button();
        this._btnDetect = new System.Windows.Forms.Button();
        this._pageInfo = new System.Windows.Forms.Panel();
        this._titleInfo = new System.Windows.Forms.Label();
        this._lblVersion = new System.Windows.Forms.Label();
        this._linkSite = new System.Windows.Forms.LinkLabel();
        this._linkGitHub = new System.Windows.Forms.LinkLabel();
        this._lblPaths = new System.Windows.Forms.Label();
        this._linkConfigFolder = new System.Windows.Forms.LinkLabel();
        this._lblLicense = new System.Windows.Forms.Label();
        this._content.SuspendLayout();
        this._footer.SuspendLayout();
        this._pageGeneral.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._numRemoteMinutes)).BeginInit();
        this._pageAccount.SuspendLayout();
        this._pageEmulators.SuspendLayout();
        this._pageInfo.SuspendLayout();
        this.SuspendLayout();
        //
        // _sections
        //
        this._sections.Dock = System.Windows.Forms.DockStyle.Left;
        this._sections.IntegralHeight = false;
        this._sections.ItemHeight = 24;
        this._sections.Items.AddRange(new object[] {
            "General",
            "Account",
            "Emulators",
            "Info"});
        this._sections.Location = new System.Drawing.Point(0, 0);
        this._sections.Name = "_sections";
        this._sections.Size = new System.Drawing.Size(170, 440);
        this._sections.TabIndex = 0;
        //
        // _content
        //
        this._content.Controls.Add(this._pageGeneral);
        this._content.Controls.Add(this._pageAccount);
        this._content.Controls.Add(this._pageEmulators);
        this._content.Controls.Add(this._pageInfo);
        this._content.Dock = System.Windows.Forms.DockStyle.Fill;
        this._content.Location = new System.Drawing.Point(170, 0);
        this._content.Name = "_content";
        this._content.Padding = new System.Windows.Forms.Padding(20, 16, 20, 8);
        this._content.Size = new System.Drawing.Size(570, 440);
        this._content.TabIndex = 1;
        //
        // _footer
        //
        this._footer.Controls.Add(this._close);
        this._footer.Dock = System.Windows.Forms.DockStyle.Bottom;
        this._footer.Location = new System.Drawing.Point(0, 440);
        this._footer.Name = "_footer";
        this._footer.Size = new System.Drawing.Size(740, 50);
        this._footer.TabIndex = 2;
        //
        // _close
        //
        this._close.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this._close.Location = new System.Drawing.Point(640, 10);
        this._close.Name = "_close";
        this._close.Size = new System.Drawing.Size(85, 30);
        this._close.TabIndex = 0;
        this._close.Text = "Close";
        this._close.UseVisualStyleBackColor = true;
        //
        // _pageGeneral
        //
        this._pageGeneral.Controls.Add(this._titleGeneral);
        this._pageGeneral.Controls.Add(this._chkAutoSync);
        this._pageGeneral.Controls.Add(this._descAutoSync);
        this._pageGeneral.Controls.Add(this._chkRemoteCheck);
        this._pageGeneral.Controls.Add(this._numRemoteMinutes);
        this._pageGeneral.Controls.Add(this._lblMinutes);
        this._pageGeneral.Controls.Add(this._descRemoteCheck);
        this._pageGeneral.Controls.Add(this._chkStartup);
        this._pageGeneral.Controls.Add(this._descStartup);
        this._pageGeneral.Dock = System.Windows.Forms.DockStyle.Fill;
        this._pageGeneral.Location = new System.Drawing.Point(20, 16);
        this._pageGeneral.Name = "_pageGeneral";
        this._pageGeneral.Size = new System.Drawing.Size(530, 416);
        this._pageGeneral.TabIndex = 0;
        //
        // _titleGeneral
        //
        this._titleGeneral.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
        this._titleGeneral.Location = new System.Drawing.Point(0, 0);
        this._titleGeneral.Name = "_titleGeneral";
        this._titleGeneral.Size = new System.Drawing.Size(520, 28);
        this._titleGeneral.TabIndex = 0;
        this._titleGeneral.Text = "General";
        //
        // _chkAutoSync
        //
        this._chkAutoSync.Location = new System.Drawing.Point(0, 44);
        this._chkAutoSync.Name = "_chkAutoSync";
        this._chkAutoSync.Size = new System.Drawing.Size(520, 24);
        this._chkAutoSync.TabIndex = 1;
        this._chkAutoSync.Text = "Sync automatically when saves change";
        this._chkAutoSync.UseVisualStyleBackColor = true;
        //
        // _descAutoSync
        //
        this._descAutoSync.ForeColor = System.Drawing.SystemColors.GrayText;
        this._descAutoSync.Location = new System.Drawing.Point(22, 68);
        this._descAutoSync.Name = "_descAutoSync";
        this._descAutoSync.Size = new System.Drawing.Size(498, 34);
        this._descAutoSync.TabIndex = 2;
        this._descAutoSync.Text = "EmuSync watches the save folders and uploads 30 seconds after the emulator stops w" +
            "riting.";
        //
        // _chkRemoteCheck
        //
        this._chkRemoteCheck.Location = new System.Drawing.Point(0, 116);
        this._chkRemoteCheck.Name = "_chkRemoteCheck";
        this._chkRemoteCheck.Size = new System.Drawing.Size(280, 24);
        this._chkRemoteCheck.TabIndex = 3;
        this._chkRemoteCheck.Text = "Check for changes from other computers";
        this._chkRemoteCheck.UseVisualStyleBackColor = true;
        //
        // _numRemoteMinutes
        //
        this._numRemoteMinutes.Location = new System.Drawing.Point(286, 117);
        this._numRemoteMinutes.Maximum = new decimal(new int[] { 720, 0, 0, 0 });
        this._numRemoteMinutes.Minimum = new decimal(new int[] { 5, 0, 0, 0 });
        this._numRemoteMinutes.Name = "_numRemoteMinutes";
        this._numRemoteMinutes.Size = new System.Drawing.Size(70, 23);
        this._numRemoteMinutes.TabIndex = 4;
        this._numRemoteMinutes.Value = new decimal(new int[] { 15, 0, 0, 0 });
        //
        // _lblMinutes
        //
        this._lblMinutes.Location = new System.Drawing.Point(362, 120);
        this._lblMinutes.Name = "_lblMinutes";
        this._lblMinutes.Size = new System.Drawing.Size(158, 20);
        this._lblMinutes.TabIndex = 5;
        this._lblMinutes.Text = "minutes";
        //
        // _descRemoteCheck
        //
        this._descRemoteCheck.ForeColor = System.Drawing.SystemColors.GrayText;
        this._descRemoteCheck.Location = new System.Drawing.Point(22, 142);
        this._descRemoteCheck.Name = "_descRemoteCheck";
        this._descRemoteCheck.Size = new System.Drawing.Size(498, 34);
        this._descRemoteCheck.TabIndex = 6;
        this._descRemoteCheck.Text = "Costs one listing per emulator: when nothing changed, nothing is transferred.";
        //
        // _chkStartup
        //
        this._chkStartup.Location = new System.Drawing.Point(0, 190);
        this._chkStartup.Name = "_chkStartup";
        this._chkStartup.Size = new System.Drawing.Size(520, 24);
        this._chkStartup.TabIndex = 7;
        this._chkStartup.Text = "Start EmuSync with Windows";
        this._chkStartup.UseVisualStyleBackColor = true;
        //
        // _descStartup
        //
        this._descStartup.ForeColor = System.Drawing.SystemColors.GrayText;
        this._descStartup.Location = new System.Drawing.Point(22, 214);
        this._descStartup.Name = "_descStartup";
        this._descStartup.Size = new System.Drawing.Size(498, 34);
        this._descStartup.TabIndex = 8;
        this._descStartup.Text = "Starts hidden in the system tray and syncs straight away.";
        //
        // _pageAccount
        //
        this._pageAccount.Controls.Add(this._titleAccount);
        this._pageAccount.Controls.Add(this._lblAccountCaption);
        this._pageAccount.Controls.Add(this._lblAccountEmail);
        this._pageAccount.Controls.Add(this._btnSignOut);
        this._pageAccount.Controls.Add(this._lblDriveCaption);
        this._pageAccount.Controls.Add(this._lblDriveState);
        this._pageAccount.Controls.Add(this._btnChangeDrive);
        this._pageAccount.Controls.Add(this._lblFolderCaption);
        this._pageAccount.Controls.Add(this._lblDriveFolder);
        this._pageAccount.Controls.Add(this._btnDriveFolder);
        this._pageAccount.Dock = System.Windows.Forms.DockStyle.Fill;
        this._pageAccount.Location = new System.Drawing.Point(20, 16);
        this._pageAccount.Name = "_pageAccount";
        this._pageAccount.Size = new System.Drawing.Size(530, 416);
        this._pageAccount.TabIndex = 1;
        //
        // _titleAccount
        //
        this._titleAccount.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
        this._titleAccount.Location = new System.Drawing.Point(0, 0);
        this._titleAccount.Name = "_titleAccount";
        this._titleAccount.Size = new System.Drawing.Size(520, 28);
        this._titleAccount.TabIndex = 0;
        this._titleAccount.Text = "Account";
        //
        // _lblAccountCaption
        //
        this._lblAccountCaption.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this._lblAccountCaption.Location = new System.Drawing.Point(0, 44);
        this._lblAccountCaption.Name = "_lblAccountCaption";
        this._lblAccountCaption.Size = new System.Drawing.Size(520, 20);
        this._lblAccountCaption.TabIndex = 1;
        this._lblAccountCaption.Text = "EmuSync account";
        //
        // _lblAccountEmail
        //
        this._lblAccountEmail.ForeColor = System.Drawing.SystemColors.GrayText;
        this._lblAccountEmail.Location = new System.Drawing.Point(0, 66);
        this._lblAccountEmail.Name = "_lblAccountEmail";
        this._lblAccountEmail.Size = new System.Drawing.Size(380, 40);
        this._lblAccountEmail.TabIndex = 2;
        //
        // _btnSignOut
        //
        this._btnSignOut.Location = new System.Drawing.Point(390, 62);
        this._btnSignOut.Name = "_btnSignOut";
        this._btnSignOut.Size = new System.Drawing.Size(130, 30);
        this._btnSignOut.TabIndex = 3;
        this._btnSignOut.Text = "Sign out";
        this._btnSignOut.UseVisualStyleBackColor = true;
        //
        // _lblDriveCaption
        //
        this._lblDriveCaption.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this._lblDriveCaption.Location = new System.Drawing.Point(0, 126);
        this._lblDriveCaption.Name = "_lblDriveCaption";
        this._lblDriveCaption.Size = new System.Drawing.Size(520, 20);
        this._lblDriveCaption.TabIndex = 4;
        this._lblDriveCaption.Text = "Google Drive";
        //
        // _lblDriveState
        //
        this._lblDriveState.ForeColor = System.Drawing.SystemColors.GrayText;
        this._lblDriveState.Location = new System.Drawing.Point(0, 148);
        this._lblDriveState.Name = "_lblDriveState";
        this._lblDriveState.Size = new System.Drawing.Size(380, 40);
        this._lblDriveState.TabIndex = 5;
        //
        // _btnChangeDrive
        //
        this._btnChangeDrive.Location = new System.Drawing.Point(390, 144);
        this._btnChangeDrive.Name = "_btnChangeDrive";
        this._btnChangeDrive.Size = new System.Drawing.Size(130, 30);
        this._btnChangeDrive.TabIndex = 6;
        this._btnChangeDrive.Text = "Change account...";
        this._btnChangeDrive.UseVisualStyleBackColor = true;
        //
        // _lblFolderCaption
        //
        this._lblFolderCaption.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this._lblFolderCaption.Location = new System.Drawing.Point(0, 208);
        this._lblFolderCaption.Name = "_lblFolderCaption";
        this._lblFolderCaption.Size = new System.Drawing.Size(520, 20);
        this._lblFolderCaption.TabIndex = 7;
        this._lblFolderCaption.Text = "Folder on Drive";
        //
        // _lblDriveFolder
        //
        this._lblDriveFolder.ForeColor = System.Drawing.SystemColors.GrayText;
        this._lblDriveFolder.Location = new System.Drawing.Point(0, 230);
        this._lblDriveFolder.Name = "_lblDriveFolder";
        this._lblDriveFolder.Size = new System.Drawing.Size(380, 40);
        this._lblDriveFolder.TabIndex = 8;
        //
        // _btnDriveFolder
        //
        this._btnDriveFolder.Location = new System.Drawing.Point(390, 226);
        this._btnDriveFolder.Name = "_btnDriveFolder";
        this._btnDriveFolder.Size = new System.Drawing.Size(130, 30);
        this._btnDriveFolder.TabIndex = 9;
        this._btnDriveFolder.Text = "Change folder...";
        this._btnDriveFolder.UseVisualStyleBackColor = true;
        //
        // _pageEmulators
        //
        this._pageEmulators.Controls.Add(this._titleEmulators);
        this._pageEmulators.Controls.Add(this._emulators);
        this._pageEmulators.Controls.Add(this._btnAdd);
        this._pageEmulators.Controls.Add(this._btnSetFolder);
        this._pageEmulators.Controls.Add(this._btnRemove);
        this._pageEmulators.Controls.Add(this._btnDetect);
        this._pageEmulators.Dock = System.Windows.Forms.DockStyle.Fill;
        this._pageEmulators.Location = new System.Drawing.Point(20, 16);
        this._pageEmulators.Name = "_pageEmulators";
        this._pageEmulators.Size = new System.Drawing.Size(530, 416);
        this._pageEmulators.TabIndex = 2;
        //
        // _titleEmulators
        //
        this._titleEmulators.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
        this._titleEmulators.Location = new System.Drawing.Point(0, 0);
        this._titleEmulators.Name = "_titleEmulators";
        this._titleEmulators.Size = new System.Drawing.Size(520, 28);
        this._titleEmulators.TabIndex = 0;
        this._titleEmulators.Text = "Emulators";
        //
        // _emulators
        //
        this._emulators.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this._colEmulator,
            this._colConsole,
            this._colFolder});
        this._emulators.FullRowSelect = true;
        this._emulators.Location = new System.Drawing.Point(0, 40);
        this._emulators.MultiSelect = false;
        this._emulators.Name = "_emulators";
        this._emulators.Size = new System.Drawing.Size(376, 370);
        this._emulators.TabIndex = 1;
        this._emulators.UseCompatibleStateImageBehavior = false;
        this._emulators.View = System.Windows.Forms.View.Details;
        //
        // _colEmulator
        //
        this._colEmulator.Text = "Emulator";
        this._colEmulator.Width = 110;
        //
        // _colConsole
        //
        this._colConsole.Text = "Console";
        this._colConsole.Width = 110;
        //
        // _colFolder
        //
        this._colFolder.Text = "Local folder";
        this._colFolder.Width = 150;
        //
        // _btnAdd
        //
        this._btnAdd.Location = new System.Drawing.Point(386, 40);
        this._btnAdd.Name = "_btnAdd";
        this._btnAdd.Size = new System.Drawing.Size(134, 30);
        this._btnAdd.TabIndex = 2;
        this._btnAdd.Text = "Add...";
        this._btnAdd.UseVisualStyleBackColor = true;
        //
        // _btnSetFolder
        //
        this._btnSetFolder.Location = new System.Drawing.Point(386, 76);
        this._btnSetFolder.Name = "_btnSetFolder";
        this._btnSetFolder.Size = new System.Drawing.Size(134, 30);
        this._btnSetFolder.TabIndex = 3;
        this._btnSetFolder.Text = "Local folder...";
        this._btnSetFolder.UseVisualStyleBackColor = true;
        //
        // _btnRemove
        //
        this._btnRemove.Location = new System.Drawing.Point(386, 112);
        this._btnRemove.Name = "_btnRemove";
        this._btnRemove.Size = new System.Drawing.Size(134, 30);
        this._btnRemove.TabIndex = 4;
        this._btnRemove.Text = "Remove";
        this._btnRemove.UseVisualStyleBackColor = true;
        //
        // _btnDetect
        //
        this._btnDetect.Location = new System.Drawing.Point(386, 158);
        this._btnDetect.Name = "_btnDetect";
        this._btnDetect.Size = new System.Drawing.Size(134, 30);
        this._btnDetect.TabIndex = 5;
        this._btnDetect.Text = "Detect on this PC";
        this._btnDetect.UseVisualStyleBackColor = true;
        //
        // _pageInfo
        //
        this._pageInfo.Controls.Add(this._titleInfo);
        this._pageInfo.Controls.Add(this._lblVersion);
        this._pageInfo.Controls.Add(this._linkSite);
        this._pageInfo.Controls.Add(this._linkGitHub);
        this._pageInfo.Controls.Add(this._lblPaths);
        this._pageInfo.Controls.Add(this._linkConfigFolder);
        this._pageInfo.Controls.Add(this._lblLicense);
        this._pageInfo.Dock = System.Windows.Forms.DockStyle.Fill;
        this._pageInfo.Location = new System.Drawing.Point(20, 16);
        this._pageInfo.Name = "_pageInfo";
        this._pageInfo.Size = new System.Drawing.Size(530, 416);
        this._pageInfo.TabIndex = 3;
        //
        // _titleInfo
        //
        this._titleInfo.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
        this._titleInfo.Location = new System.Drawing.Point(0, 0);
        this._titleInfo.Name = "_titleInfo";
        this._titleInfo.Size = new System.Drawing.Size(520, 28);
        this._titleInfo.TabIndex = 0;
        this._titleInfo.Text = "Info";
        //
        // _lblVersion
        //
        this._lblVersion.Location = new System.Drawing.Point(0, 44);
        this._lblVersion.Name = "_lblVersion";
        this._lblVersion.Size = new System.Drawing.Size(520, 24);
        this._lblVersion.TabIndex = 1;
        //
        // _linkSite
        //
        this._linkSite.Location = new System.Drawing.Point(0, 76);
        this._linkSite.Name = "_linkSite";
        this._linkSite.Size = new System.Drawing.Size(300, 20);
        this._linkSite.TabIndex = 2;
        this._linkSite.TabStop = true;
        this._linkSite.Text = "emusync-43d2b.web.app";
        //
        // _linkGitHub
        //
        this._linkGitHub.Location = new System.Drawing.Point(0, 100);
        this._linkGitHub.Name = "_linkGitHub";
        this._linkGitHub.Size = new System.Drawing.Size(300, 20);
        this._linkGitHub.TabIndex = 3;
        this._linkGitHub.TabStop = true;
        this._linkGitHub.Text = "github.com/Weskerw/EmuSync";
        //
        // _lblPaths
        //
        this._lblPaths.ForeColor = System.Drawing.SystemColors.GrayText;
        this._lblPaths.Location = new System.Drawing.Point(0, 140);
        this._lblPaths.Name = "_lblPaths";
        this._lblPaths.Size = new System.Drawing.Size(520, 60);
        this._lblPaths.TabIndex = 4;
        //
        // _linkConfigFolder
        //
        this._linkConfigFolder.Location = new System.Drawing.Point(0, 200);
        this._linkConfigFolder.Name = "_linkConfigFolder";
        this._linkConfigFolder.Size = new System.Drawing.Size(300, 20);
        this._linkConfigFolder.TabIndex = 5;
        this._linkConfigFolder.TabStop = true;
        this._linkConfigFolder.Text = "Open the settings folder";
        //
        // _lblLicense
        //
        this._lblLicense.ForeColor = System.Drawing.SystemColors.GrayText;
        this._lblLicense.Location = new System.Drawing.Point(0, 240);
        this._lblLicense.Name = "_lblLicense";
        this._lblLicense.Size = new System.Drawing.Size(520, 40);
        this._lblLicense.TabIndex = 6;
        this._lblLicense.Text = "MIT licence · an independent open-source project, not affiliated with Google or wi" +
            "th any emulator project.";
        //
        // SettingsForm
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(740, 490);
        this.Controls.Add(this._content);
        this.Controls.Add(this._sections);
        this.Controls.Add(this._footer);
        this.MinimizeBox = false;
        this.MinimumSize = new System.Drawing.Size(700, 460);
        this.Name = "SettingsForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "EmuSync – Settings";
        this._content.ResumeLayout(false);
        this._footer.ResumeLayout(false);
        this._pageGeneral.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this._numRemoteMinutes)).EndInit();
        this._pageAccount.ResumeLayout(false);
        this._pageEmulators.ResumeLayout(false);
        this._pageInfo.ResumeLayout(false);
        this.ResumeLayout(false);
    }

    #endregion
}
