namespace EmuSync;

partial class MainForm
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null!;

    private System.Windows.Forms.MenuStrip _menu;
    private System.Windows.Forms.ToolStripMenuItem _syncMenu;
    private System.Windows.Forms.ToolStripMenuItem _miAdd;
    private System.Windows.Forms.ToolStripMenuItem _miSetFolder;
    private System.Windows.Forms.ToolStripMenuItem _miRemove;
    private System.Windows.Forms.ToolStripSeparator _sepFolders;
    private System.Windows.Forms.ToolStripMenuItem _miSyncSelected;
    private System.Windows.Forms.ToolStripMenuItem _miSyncAll;
    private System.Windows.Forms.ToolStripSeparator _sepAuto;
    private System.Windows.Forms.ToolStripMenuItem _miAuto;
    private System.Windows.Forms.ToolStripSeparator _sepHistory;
    private System.Windows.Forms.ToolStripMenuItem _miHistory;
    private System.Windows.Forms.ToolStripMenuItem _settingsMenu;
    private System.Windows.Forms.ToolStripMenuItem _miDetect;
    private System.Windows.Forms.ToolStripSeparator _sepAccount;
    private System.Windows.Forms.ToolStripMenuItem _miChangeAccount;
    private System.Windows.Forms.ToolStripMenuItem _miSignOut;
    private System.Windows.Forms.ToolStripSeparator _sepStartup;
    private System.Windows.Forms.ToolStripMenuItem _miStartWithWindows;

    private System.Windows.Forms.SplitContainer _split;
    private System.Windows.Forms.ListView _list;
    private System.Windows.Forms.ColumnHeader _colEmulator;
    private System.Windows.Forms.ColumnHeader _colConsole;
    private System.Windows.Forms.ColumnHeader _colFolder;
    private System.Windows.Forms.ColumnHeader _colLastSync;
    private System.Windows.Forms.TextBox _log;

    private System.Windows.Forms.StatusStrip _statusBar;
    private System.Windows.Forms.ToolStripStatusLabel _lblAccount;
    private System.Windows.Forms.ToolStripStatusLabel _lblDrive;

    private System.Windows.Forms.NotifyIcon _tray;
    private System.Windows.Forms.ContextMenuStrip _trayMenu;
    private System.Windows.Forms.ToolStripMenuItem _trayOpen;
    private System.Windows.Forms.ToolStripMenuItem _traySyncAll;
    private System.Windows.Forms.ToolStripSeparator _traySep;
    private System.Windows.Forms.ToolStripMenuItem _trayExit;
    private System.Windows.Forms.Timer _autoSyncTimer;
    private System.Windows.Forms.Timer _remoteCheckTimer;

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
        this._menu = new System.Windows.Forms.MenuStrip();
        this._syncMenu = new System.Windows.Forms.ToolStripMenuItem();
        this._miAdd = new System.Windows.Forms.ToolStripMenuItem();
        this._miSetFolder = new System.Windows.Forms.ToolStripMenuItem();
        this._miRemove = new System.Windows.Forms.ToolStripMenuItem();
        this._sepFolders = new System.Windows.Forms.ToolStripSeparator();
        this._miSyncSelected = new System.Windows.Forms.ToolStripMenuItem();
        this._miSyncAll = new System.Windows.Forms.ToolStripMenuItem();
        this._sepAuto = new System.Windows.Forms.ToolStripSeparator();
        this._miAuto = new System.Windows.Forms.ToolStripMenuItem();
        this._sepHistory = new System.Windows.Forms.ToolStripSeparator();
        this._miHistory = new System.Windows.Forms.ToolStripMenuItem();
        this._settingsMenu = new System.Windows.Forms.ToolStripMenuItem();
        this._miDetect = new System.Windows.Forms.ToolStripMenuItem();
        this._sepAccount = new System.Windows.Forms.ToolStripSeparator();
        this._miChangeAccount = new System.Windows.Forms.ToolStripMenuItem();
        this._miSignOut = new System.Windows.Forms.ToolStripMenuItem();
        this._sepStartup = new System.Windows.Forms.ToolStripSeparator();
        this._miStartWithWindows = new System.Windows.Forms.ToolStripMenuItem();
        this._split = new System.Windows.Forms.SplitContainer();
        this._list = new System.Windows.Forms.ListView();
        this._colEmulator = new System.Windows.Forms.ColumnHeader();
        this._colConsole = new System.Windows.Forms.ColumnHeader();
        this._colFolder = new System.Windows.Forms.ColumnHeader();
        this._colLastSync = new System.Windows.Forms.ColumnHeader();
        this._log = new System.Windows.Forms.TextBox();
        this._statusBar = new System.Windows.Forms.StatusStrip();
        this._lblAccount = new System.Windows.Forms.ToolStripStatusLabel();
        this._lblDrive = new System.Windows.Forms.ToolStripStatusLabel();
        this._trayMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
        this._trayOpen = new System.Windows.Forms.ToolStripMenuItem();
        this._traySyncAll = new System.Windows.Forms.ToolStripMenuItem();
        this._traySep = new System.Windows.Forms.ToolStripSeparator();
        this._trayExit = new System.Windows.Forms.ToolStripMenuItem();
        this._tray = new System.Windows.Forms.NotifyIcon(this.components);
        this._autoSyncTimer = new System.Windows.Forms.Timer(this.components);
        this._remoteCheckTimer = new System.Windows.Forms.Timer(this.components);
        this._menu.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._split)).BeginInit();
        this._split.Panel1.SuspendLayout();
        this._split.Panel2.SuspendLayout();
        this._split.SuspendLayout();
        this._statusBar.SuspendLayout();
        this._trayMenu.SuspendLayout();
        this.SuspendLayout();
        //
        // _menu
        //
        this._menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._syncMenu,
            this._settingsMenu});
        this._menu.Location = new System.Drawing.Point(0, 0);
        this._menu.Name = "_menu";
        this._menu.Size = new System.Drawing.Size(860, 24);
        this._menu.TabIndex = 0;
        this._menu.Text = "menuStrip";
        //
        // _syncMenu
        //
        this._syncMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._miAdd,
            this._miSetFolder,
            this._miRemove,
            this._sepFolders,
            this._miSyncSelected,
            this._miSyncAll,
            this._sepAuto,
            this._miAuto,
            this._sepHistory,
            this._miHistory});
        this._syncMenu.Name = "_syncMenu";
        this._syncMenu.Size = new System.Drawing.Size(44, 20);
        this._syncMenu.Text = "&Sync";
        //
        // _miAdd
        //
        this._miAdd.Name = "_miAdd";
        this._miAdd.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
        this._miAdd.Size = new System.Drawing.Size(280, 22);
        this._miAdd.Text = "&Add emulator...";
        //
        // _miSetFolder
        //
        this._miSetFolder.Name = "_miSetFolder";
        this._miSetFolder.Size = new System.Drawing.Size(280, 22);
        this._miSetFolder.Text = "Set &local folder...";
        //
        // _miRemove
        //
        this._miRemove.Name = "_miRemove";
        this._miRemove.ShortcutKeys = System.Windows.Forms.Keys.Delete;
        this._miRemove.Size = new System.Drawing.Size(280, 22);
        this._miRemove.Text = "&Remove";
        //
        // _sepFolders
        //
        this._sepFolders.Name = "_sepFolders";
        this._sepFolders.Size = new System.Drawing.Size(277, 6);
        //
        // _miSyncSelected
        //
        this._miSyncSelected.Name = "_miSyncSelected";
        this._miSyncSelected.ShortcutKeys = System.Windows.Forms.Keys.F5;
        this._miSyncSelected.Size = new System.Drawing.Size(280, 22);
        this._miSyncSelected.Text = "Sync se&lected";
        //
        // _miSyncAll
        //
        this._miSyncAll.Name = "_miSyncAll";
        this._miSyncAll.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F5)));
        this._miSyncAll.Size = new System.Drawing.Size(280, 22);
        this._miSyncAll.Text = "Sync a&ll";
        //
        // _sepAuto
        //
        this._sepAuto.Name = "_sepAuto";
        this._sepAuto.Size = new System.Drawing.Size(277, 6);
        //
        // _miAuto
        //
        this._miAuto.CheckOnClick = true;
        this._miAuto.Name = "_miAuto";
        this._miAuto.Size = new System.Drawing.Size(280, 22);
        this._miAuto.Text = "A&uto-sync when saves change";
        //
        // _sepHistory
        //
        this._sepHistory.Name = "_sepHistory";
        this._sepHistory.Size = new System.Drawing.Size(277, 6);
        //
        // _miHistory
        //
        this._miHistory.Name = "_miHistory";
        this._miHistory.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.H)));
        this._miHistory.Size = new System.Drawing.Size(280, 22);
        this._miHistory.Text = "Sync &history...";
        //
        // _settingsMenu
        //
        this._settingsMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._miDetect,
            this._sepAccount,
            this._miChangeAccount,
            this._miSignOut,
            this._sepStartup,
            this._miStartWithWindows});
        this._settingsMenu.Name = "_settingsMenu";
        this._settingsMenu.Size = new System.Drawing.Size(61, 20);
        this._settingsMenu.Text = "S&ettings";
        //
        // _miDetect
        //
        this._miDetect.Name = "_miDetect";
        this._miDetect.Size = new System.Drawing.Size(268, 22);
        this._miDetect.Text = "&Detect emulators on this PC...";
        //
        // _sepAccount
        //
        this._sepAccount.Name = "_sepAccount";
        this._sepAccount.Size = new System.Drawing.Size(265, 6);
        //
        // _miChangeAccount
        //
        this._miChangeAccount.Name = "_miChangeAccount";
        this._miChangeAccount.Size = new System.Drawing.Size(268, 22);
        this._miChangeAccount.Text = "Change &Google Drive account...";
        //
        // _miSignOut
        //
        this._miSignOut.Name = "_miSignOut";
        this._miSignOut.Size = new System.Drawing.Size(268, 22);
        this._miSignOut.Text = "Sign &out of EmuSync";
        //
        // _sepStartup
        //
        this._sepStartup.Name = "_sepStartup";
        this._sepStartup.Size = new System.Drawing.Size(265, 6);
        //
        // _miStartWithWindows
        //
        this._miStartWithWindows.CheckOnClick = true;
        this._miStartWithWindows.Name = "_miStartWithWindows";
        this._miStartWithWindows.Size = new System.Drawing.Size(268, 22);
        this._miStartWithWindows.Text = "Start with Windows";
        //
        // _split
        //
        this._split.Dock = System.Windows.Forms.DockStyle.Fill;
        this._split.Location = new System.Drawing.Point(0, 24);
        this._split.Name = "_split";
        this._split.Orientation = System.Windows.Forms.Orientation.Horizontal;
        //
        // _split.Panel1
        //
        this._split.Panel1.Controls.Add(this._list);
        //
        // _split.Panel2
        //
        this._split.Panel2.Controls.Add(this._log);
        this._split.Size = new System.Drawing.Size(860, 465);
        this._split.SplitterDistance = 230;
        this._split.TabIndex = 1;
        //
        // _list
        //
        this._list.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this._colEmulator,
            this._colConsole,
            this._colFolder,
            this._colLastSync});
        this._list.Dock = System.Windows.Forms.DockStyle.Fill;
        this._list.FullRowSelect = true;
        this._list.Location = new System.Drawing.Point(0, 0);
        this._list.MultiSelect = false;
        this._list.Name = "_list";
        this._list.Size = new System.Drawing.Size(860, 230);
        this._list.TabIndex = 0;
        this._list.UseCompatibleStateImageBehavior = false;
        this._list.View = System.Windows.Forms.View.Details;
        //
        // _colEmulator
        //
        this._colEmulator.Text = "Emulator";
        this._colEmulator.Width = 130;
        //
        // _colConsole
        //
        this._colConsole.Text = "Console";
        this._colConsole.Width = 140;
        //
        // _colFolder
        //
        this._colFolder.Text = "Local folder";
        this._colFolder.Width = 400;
        //
        // _colLastSync
        //
        this._colLastSync.Text = "Last sync";
        this._colLastSync.Width = 150;
        //
        // _log
        //
        this._log.Dock = System.Windows.Forms.DockStyle.Fill;
        this._log.Font = new System.Drawing.Font("Consolas", 9F);
        this._log.Location = new System.Drawing.Point(0, 0);
        this._log.Multiline = true;
        this._log.Name = "_log";
        this._log.ReadOnly = true;
        this._log.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        this._log.Size = new System.Drawing.Size(860, 231);
        this._log.TabIndex = 0;
        //
        // _statusBar
        //
        this._statusBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._lblAccount,
            this._lblDrive});
        this._statusBar.Location = new System.Drawing.Point(0, 489);
        this._statusBar.Name = "_statusBar";
        this._statusBar.Size = new System.Drawing.Size(860, 22);
        this._statusBar.TabIndex = 2;
        //
        // _lblAccount
        //
        this._lblAccount.Name = "_lblAccount";
        this._lblAccount.Size = new System.Drawing.Size(120, 17);
        this._lblAccount.Text = "Not signed in";
        //
        // _lblDrive
        //
        this._lblDrive.Name = "_lblDrive";
        this._lblDrive.Size = new System.Drawing.Size(120, 17);
        this._lblDrive.Text = "Drive: not connected";
        //
        // _trayMenu
        //
        this._trayMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._trayOpen,
            this._traySyncAll,
            this._traySep,
            this._trayExit});
        this._trayMenu.Name = "_trayMenu";
        this._trayMenu.Size = new System.Drawing.Size(153, 76);
        //
        // _trayOpen
        //
        this._trayOpen.Name = "_trayOpen";
        this._trayOpen.Size = new System.Drawing.Size(152, 22);
        this._trayOpen.Text = "Open EmuSync";
        //
        // _traySyncAll
        //
        this._traySyncAll.Name = "_traySyncAll";
        this._traySyncAll.Size = new System.Drawing.Size(152, 22);
        this._traySyncAll.Text = "Sync all now";
        //
        // _traySep
        //
        this._traySep.Name = "_traySep";
        this._traySep.Size = new System.Drawing.Size(149, 6);
        //
        // _trayExit
        //
        this._trayExit.Name = "_trayExit";
        this._trayExit.Size = new System.Drawing.Size(152, 22);
        this._trayExit.Text = "Exit";
        //
        // _tray
        //
        this._tray.ContextMenuStrip = this._trayMenu;
        this._tray.Text = "EmuSync";
        this._tray.Visible = true;
        //
        // _autoSyncTimer
        //
        this._autoSyncTimer.Interval = 10000;
        //
        // MainForm
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(860, 511);
        this.Controls.Add(this._split);
        this.Controls.Add(this._statusBar);
        this.Controls.Add(this._menu);
        this.MainMenuStrip = this._menu;
        this.MinimumSize = new System.Drawing.Size(760, 480);
        this.Name = "MainForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "EmuSync – Emulator saves, synced";
        this._menu.ResumeLayout(false);
        this._menu.PerformLayout();
        this._split.Panel1.ResumeLayout(false);
        this._split.Panel2.ResumeLayout(false);
        this._split.Panel2.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this._split)).EndInit();
        this._split.ResumeLayout(false);
        this._statusBar.ResumeLayout(false);
        this._statusBar.PerformLayout();
        this._trayMenu.ResumeLayout(false);
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion
}
