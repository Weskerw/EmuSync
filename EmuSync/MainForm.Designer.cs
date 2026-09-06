namespace EmuSync;

partial class MainForm
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null!;

    private System.Windows.Forms.MenuStrip _menu;
    private System.Windows.Forms.ToolStripMenuItem _syncMenu;
    private System.Windows.Forms.ToolStripMenuItem _miAdd;
    private System.Windows.Forms.ToolStripMenuItem _miRemove;
    private System.Windows.Forms.ToolStripSeparator _sepFolders;
    private System.Windows.Forms.ToolStripMenuItem _miSyncSelected;
    private System.Windows.Forms.ToolStripMenuItem _miSyncAll;
    private System.Windows.Forms.ToolStripSeparator _sepAuto;
    private System.Windows.Forms.ToolStripMenuItem _miAuto;
    private System.Windows.Forms.ToolStripMenuItem _settingsMenu;
    private System.Windows.Forms.ToolStripMenuItem _miChangeAccount;
    private System.Windows.Forms.ToolStripMenuItem _miStartWithWindows;

    private System.Windows.Forms.SplitContainer _split;
    private System.Windows.Forms.ListView _list;
    private System.Windows.Forms.ColumnHeader _colProfile;
    private System.Windows.Forms.ColumnHeader _colFolder;
    private System.Windows.Forms.ColumnHeader _colLastSync;
    private System.Windows.Forms.TextBox _log;

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
        this._miRemove = new System.Windows.Forms.ToolStripMenuItem();
        this._sepFolders = new System.Windows.Forms.ToolStripSeparator();
        this._miSyncSelected = new System.Windows.Forms.ToolStripMenuItem();
        this._miSyncAll = new System.Windows.Forms.ToolStripMenuItem();
        this._sepAuto = new System.Windows.Forms.ToolStripSeparator();
        this._miAuto = new System.Windows.Forms.ToolStripMenuItem();
        this._settingsMenu = new System.Windows.Forms.ToolStripMenuItem();
        this._miChangeAccount = new System.Windows.Forms.ToolStripMenuItem();
        this._miStartWithWindows = new System.Windows.Forms.ToolStripMenuItem();
        this._split = new System.Windows.Forms.SplitContainer();
        this._list = new System.Windows.Forms.ListView();
        this._colProfile = new System.Windows.Forms.ColumnHeader();
        this._colFolder = new System.Windows.Forms.ColumnHeader();
        this._colLastSync = new System.Windows.Forms.ColumnHeader();
        this._log = new System.Windows.Forms.TextBox();
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
        this._menu.Size = new System.Drawing.Size(784, 24);
        this._menu.TabIndex = 0;
        this._menu.Text = "menuStrip";
        //
        // _syncMenu
        //
        this._syncMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._miAdd,
            this._miRemove,
            this._sepFolders,
            this._miSyncSelected,
            this._miSyncAll,
            this._sepAuto,
            this._miAuto});
        this._syncMenu.Name = "_syncMenu";
        this._syncMenu.Size = new System.Drawing.Size(44, 20);
        this._syncMenu.Text = "&Sync";
        //
        // _miAdd
        //
        this._miAdd.Name = "_miAdd";
        this._miAdd.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
        this._miAdd.Size = new System.Drawing.Size(250, 22);
        this._miAdd.Text = "&Add folder...";
        //
        // _miRemove
        //
        this._miRemove.Name = "_miRemove";
        this._miRemove.ShortcutKeys = System.Windows.Forms.Keys.Delete;
        this._miRemove.Size = new System.Drawing.Size(250, 22);
        this._miRemove.Text = "&Remove";
        //
        // _sepFolders
        //
        this._sepFolders.Name = "_sepFolders";
        this._sepFolders.Size = new System.Drawing.Size(247, 6);
        //
        // _miSyncSelected
        //
        this._miSyncSelected.Name = "_miSyncSelected";
        this._miSyncSelected.ShortcutKeys = System.Windows.Forms.Keys.F5;
        this._miSyncSelected.Size = new System.Drawing.Size(250, 22);
        this._miSyncSelected.Text = "Sync se&lected";
        //
        // _miSyncAll
        //
        this._miSyncAll.Name = "_miSyncAll";
        this._miSyncAll.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F5)));
        this._miSyncAll.Size = new System.Drawing.Size(250, 22);
        this._miSyncAll.Text = "Sync a&ll";
        //
        // _sepAuto
        //
        this._sepAuto.Name = "_sepAuto";
        this._sepAuto.Size = new System.Drawing.Size(247, 6);
        //
        // _miAuto
        //
        this._miAuto.CheckOnClick = true;
        this._miAuto.Name = "_miAuto";
        this._miAuto.Size = new System.Drawing.Size(250, 22);
        this._miAuto.Text = "A&uto-sync when saves change";
        //
        // _settingsMenu
        //
        this._settingsMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._miChangeAccount,
            this._miStartWithWindows});
        this._settingsMenu.Name = "_settingsMenu";
        this._settingsMenu.Size = new System.Drawing.Size(61, 20);
        this._settingsMenu.Text = "S&ettings";
        //
        // _miChangeAccount
        //
        this._miChangeAccount.Name = "_miChangeAccount";
        this._miChangeAccount.Size = new System.Drawing.Size(228, 22);
        this._miChangeAccount.Text = "Change Google account...";
        //
        // _miStartWithWindows
        //
        this._miStartWithWindows.CheckOnClick = true;
        this._miStartWithWindows.Name = "_miStartWithWindows";
        this._miStartWithWindows.Size = new System.Drawing.Size(228, 22);
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
        this._split.Size = new System.Drawing.Size(784, 487);
        this._split.SplitterDistance = 240;
        this._split.TabIndex = 1;
        //
        // _list
        //
        this._list.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this._colProfile,
            this._colFolder,
            this._colLastSync});
        this._list.Dock = System.Windows.Forms.DockStyle.Fill;
        this._list.FullRowSelect = true;
        this._list.Location = new System.Drawing.Point(0, 0);
        this._list.MultiSelect = false;
        this._list.Name = "_list";
        this._list.Size = new System.Drawing.Size(784, 240);
        this._list.TabIndex = 0;
        this._list.UseCompatibleStateImageBehavior = false;
        this._list.View = System.Windows.Forms.View.Details;
        //
        // _colProfile
        //
        this._colProfile.Text = "Profile";
        this._colProfile.Width = 150;
        //
        // _colFolder
        //
        this._colFolder.Text = "Local folder";
        this._colFolder.Width = 330;
        //
        // _colLastSync
        //
        this._colLastSync.Text = "Last sync";
        this._colLastSync.Width = 160;
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
        this._log.Size = new System.Drawing.Size(784, 243);
        this._log.TabIndex = 0;
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
        this.ClientSize = new System.Drawing.Size(784, 511);
        this.Controls.Add(this._split);
        this.Controls.Add(this._menu);
        this.MainMenuStrip = this._menu;
        this.MinimumSize = new System.Drawing.Size(720, 480);
        this.Name = "MainForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "EmuSync – Sync emulator saves with Google Drive";
        this._menu.ResumeLayout(false);
        this._menu.PerformLayout();
        this._split.Panel1.ResumeLayout(false);
        this._split.Panel2.ResumeLayout(false);
        this._split.Panel2.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this._split)).EndInit();
        this._split.ResumeLayout(false);
        this._trayMenu.ResumeLayout(false);
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion
}
