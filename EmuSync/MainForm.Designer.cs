namespace EmuSync;

partial class MainForm
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null!;

    // The menu bar holds only what is not a day-to-day action: the settings
    // window and quitting. Everything else is one click away on the toolbar,
    // and the settings that used to be checkable menu items now live in the
    // settings window, where a checkbox looks like a checkbox.
    private System.Windows.Forms.MenuStrip _menu;
    private System.Windows.Forms.ToolStripMenuItem _miSettings;
    private System.Windows.Forms.ToolStripMenuItem _miExit;

    private System.Windows.Forms.ToolStrip _toolbar;
    private System.Windows.Forms.ToolStripButton _tbSyncAll;
    private System.Windows.Forms.ToolStripButton _tbSyncSelected;
    private System.Windows.Forms.ToolStripSeparator _tbSep1;
    private System.Windows.Forms.ToolStripButton _tbAdd;
    private System.Windows.Forms.ToolStripButton _tbSetFolder;
    private System.Windows.Forms.ToolStripButton _tbRemove;
    private System.Windows.Forms.ToolStripSeparator _tbSep2;
    private System.Windows.Forms.ToolStripButton _tbHistory;

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
        this._miSettings = new System.Windows.Forms.ToolStripMenuItem();
        this._miExit = new System.Windows.Forms.ToolStripMenuItem();
        this._toolbar = new System.Windows.Forms.ToolStrip();
        this._tbSyncAll = new System.Windows.Forms.ToolStripButton();
        this._tbSyncSelected = new System.Windows.Forms.ToolStripButton();
        this._tbSep1 = new System.Windows.Forms.ToolStripSeparator();
        this._tbAdd = new System.Windows.Forms.ToolStripButton();
        this._tbSetFolder = new System.Windows.Forms.ToolStripButton();
        this._tbRemove = new System.Windows.Forms.ToolStripButton();
        this._tbSep2 = new System.Windows.Forms.ToolStripSeparator();
        this._tbHistory = new System.Windows.Forms.ToolStripButton();
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
        this._toolbar.SuspendLayout();
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
            this._miSettings,
            this._miExit});
        this._menu.Location = new System.Drawing.Point(0, 0);
        this._menu.Name = "_menu";
        this._menu.Size = new System.Drawing.Size(860, 24);
        this._menu.TabIndex = 0;
        //
        // _miSettings
        //
        this._miSettings.Name = "_miSettings";
        this._miSettings.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Oemcomma)));
        this._miSettings.Size = new System.Drawing.Size(61, 20);
        this._miSettings.Text = "&Settings";
        //
        // _miExit
        //
        this._miExit.Name = "_miExit";
        this._miExit.Size = new System.Drawing.Size(38, 20);
        this._miExit.Text = "E&xit";
        //
        // _toolbar
        //
        this._toolbar.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
        this._toolbar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._tbSyncAll,
            this._tbSyncSelected,
            this._tbSep1,
            this._tbAdd,
            this._tbSetFolder,
            this._tbRemove,
            this._tbSep2,
            this._tbHistory});
        this._toolbar.Location = new System.Drawing.Point(0, 24);
        this._toolbar.Name = "_toolbar";
        this._toolbar.Padding = new System.Windows.Forms.Padding(4, 2, 4, 2);
        this._toolbar.Size = new System.Drawing.Size(860, 27);
        this._toolbar.TabIndex = 1;
        //
        // _tbSyncAll
        //
        this._tbSyncAll.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this._tbSyncAll.Name = "_tbSyncAll";
        this._tbSyncAll.Size = new System.Drawing.Size(60, 22);
        this._tbSyncAll.Text = "Sync all";
        this._tbSyncAll.ToolTipText = "Sync every emulator now (Ctrl+F5)";
        //
        // _tbSyncSelected
        //
        this._tbSyncSelected.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this._tbSyncSelected.Name = "_tbSyncSelected";
        this._tbSyncSelected.Size = new System.Drawing.Size(90, 22);
        this._tbSyncSelected.Text = "Sync selected";
        this._tbSyncSelected.ToolTipText = "Sync the selected emulator (F5)";
        //
        // _tbSep1
        //
        this._tbSep1.Name = "_tbSep1";
        this._tbSep1.Size = new System.Drawing.Size(6, 25);
        //
        // _tbAdd
        //
        this._tbAdd.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this._tbAdd.Name = "_tbAdd";
        this._tbAdd.Size = new System.Drawing.Size(95, 22);
        this._tbAdd.Text = "Add emulator...";
        //
        // _tbSetFolder
        //
        this._tbSetFolder.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this._tbSetFolder.Name = "_tbSetFolder";
        this._tbSetFolder.Size = new System.Drawing.Size(90, 22);
        this._tbSetFolder.Text = "Local folder...";
        this._tbSetFolder.ToolTipText = "Choose the save folder on this PC for the selected emulator";
        //
        // _tbRemove
        //
        this._tbRemove.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this._tbRemove.Name = "_tbRemove";
        this._tbRemove.Size = new System.Drawing.Size(60, 22);
        this._tbRemove.Text = "Remove";
        //
        // _tbSep2
        //
        this._tbSep2.Name = "_tbSep2";
        this._tbSep2.Size = new System.Drawing.Size(6, 25);
        //
        // _tbHistory
        //
        this._tbHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this._tbHistory.Name = "_tbHistory";
        this._tbHistory.Size = new System.Drawing.Size(55, 22);
        this._tbHistory.Text = "History";
        this._tbHistory.ToolTipText = "What every device synced, and when (Ctrl+H)";
        //
        // _split
        //
        this._split.Dock = System.Windows.Forms.DockStyle.Fill;
        this._split.Location = new System.Drawing.Point(0, 51);
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
        this._split.Size = new System.Drawing.Size(860, 438);
        this._split.SplitterDistance = 215;
        this._split.TabIndex = 2;
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
        this._list.Size = new System.Drawing.Size(860, 215);
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
        this._log.Size = new System.Drawing.Size(860, 219);
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
        this._statusBar.TabIndex = 3;
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
        this.Controls.Add(this._toolbar);
        this.Controls.Add(this._menu);
        this.MainMenuStrip = this._menu;
        this.MinimumSize = new System.Drawing.Size(760, 480);
        this.Name = "MainForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "EmuSync – Emulator saves, synced";
        this._menu.ResumeLayout(false);
        this._menu.PerformLayout();
        this._toolbar.ResumeLayout(false);
        this._toolbar.PerformLayout();
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
