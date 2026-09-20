namespace EmuSync;

partial class HistoryForm
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null!;

    private System.Windows.Forms.ToolStrip _toolbar;
    private System.Windows.Forms.ToolStripButton _refresh;
    private System.Windows.Forms.ToolStripSeparator _sepFilter;
    private System.Windows.Forms.ToolStripLabel _lblFilter;
    private System.Windows.Forms.ToolStripComboBox _filter;
    private System.Windows.Forms.ToolStripSeparator _sepStatus;
    private System.Windows.Forms.ToolStripLabel _status;

    private System.Windows.Forms.SplitContainer _split;

    private System.Windows.Forms.ListView _runs;
    private System.Windows.Forms.ColumnHeader _colWhen;
    private System.Windows.Forms.ColumnHeader _colEmulator;
    private System.Windows.Forms.ColumnHeader _colDevice;
    private System.Windows.Forms.ColumnHeader _colResult;
    private System.Windows.Forms.ColumnHeader _colBytes;
    private System.Windows.Forms.ColumnHeader _colDuration;

    private System.Windows.Forms.Label _detailHeader;
    private System.Windows.Forms.ListView _operations;
    private System.Windows.Forms.ColumnHeader _colAction;
    private System.Windows.Forms.ColumnHeader _colFile;
    private System.Windows.Forms.ColumnHeader _colSize;
    private System.Windows.Forms.ColumnHeader _colTime;

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
        this._toolbar = new System.Windows.Forms.ToolStrip();
        this._refresh = new System.Windows.Forms.ToolStripButton();
        this._sepFilter = new System.Windows.Forms.ToolStripSeparator();
        this._lblFilter = new System.Windows.Forms.ToolStripLabel();
        this._filter = new System.Windows.Forms.ToolStripComboBox();
        this._sepStatus = new System.Windows.Forms.ToolStripSeparator();
        this._status = new System.Windows.Forms.ToolStripLabel();
        this._split = new System.Windows.Forms.SplitContainer();
        this._runs = new System.Windows.Forms.ListView();
        this._colWhen = new System.Windows.Forms.ColumnHeader();
        this._colEmulator = new System.Windows.Forms.ColumnHeader();
        this._colDevice = new System.Windows.Forms.ColumnHeader();
        this._colResult = new System.Windows.Forms.ColumnHeader();
        this._colBytes = new System.Windows.Forms.ColumnHeader();
        this._colDuration = new System.Windows.Forms.ColumnHeader();
        this._detailHeader = new System.Windows.Forms.Label();
        this._operations = new System.Windows.Forms.ListView();
        this._colAction = new System.Windows.Forms.ColumnHeader();
        this._colFile = new System.Windows.Forms.ColumnHeader();
        this._colSize = new System.Windows.Forms.ColumnHeader();
        this._colTime = new System.Windows.Forms.ColumnHeader();
        this._toolbar.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._split)).BeginInit();
        this._split.Panel1.SuspendLayout();
        this._split.Panel2.SuspendLayout();
        this._split.SuspendLayout();
        this.SuspendLayout();
        //
        // _toolbar
        //
        this._toolbar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._refresh,
            this._sepFilter,
            this._lblFilter,
            this._filter,
            this._sepStatus,
            this._status});
        this._toolbar.Location = new System.Drawing.Point(0, 0);
        this._toolbar.Name = "_toolbar";
        this._toolbar.Size = new System.Drawing.Size(900, 25);
        this._toolbar.TabIndex = 0;
        //
        // _refresh
        //
        this._refresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this._refresh.Name = "_refresh";
        this._refresh.Size = new System.Drawing.Size(50, 22);
        this._refresh.Text = "Refresh";
        //
        // _sepFilter
        //
        this._sepFilter.Name = "_sepFilter";
        this._sepFilter.Size = new System.Drawing.Size(6, 25);
        //
        // _lblFilter
        //
        this._lblFilter.Name = "_lblFilter";
        this._lblFilter.Size = new System.Drawing.Size(60, 22);
        this._lblFilter.Text = "Emulator:";
        //
        // _filter
        //
        this._filter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._filter.Name = "_filter";
        this._filter.Size = new System.Drawing.Size(200, 25);
        //
        // _sepStatus
        //
        this._sepStatus.Name = "_sepStatus";
        this._sepStatus.Size = new System.Drawing.Size(6, 25);
        //
        // _status
        //
        this._status.Name = "_status";
        this._status.Size = new System.Drawing.Size(0, 22);
        //
        // _split
        //
        this._split.Dock = System.Windows.Forms.DockStyle.Fill;
        this._split.Location = new System.Drawing.Point(0, 25);
        this._split.Name = "_split";
        this._split.Orientation = System.Windows.Forms.Orientation.Horizontal;
        //
        // _split.Panel1
        //
        this._split.Panel1.Controls.Add(this._runs);
        //
        // _split.Panel2
        //
        this._split.Panel2.Controls.Add(this._operations);
        this._split.Panel2.Controls.Add(this._detailHeader);
        this._split.Size = new System.Drawing.Size(900, 535);
        this._split.SplitterDistance = 290;
        this._split.TabIndex = 1;
        //
        // _runs
        //
        this._runs.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this._colWhen,
            this._colEmulator,
            this._colDevice,
            this._colResult,
            this._colBytes,
            this._colDuration});
        this._runs.Dock = System.Windows.Forms.DockStyle.Fill;
        this._runs.FullRowSelect = true;
        this._runs.Location = new System.Drawing.Point(0, 0);
        this._runs.MultiSelect = false;
        this._runs.Name = "_runs";
        this._runs.Size = new System.Drawing.Size(900, 290);
        this._runs.TabIndex = 0;
        this._runs.UseCompatibleStateImageBehavior = false;
        this._runs.View = System.Windows.Forms.View.Details;
        //
        // _colWhen
        //
        this._colWhen.Text = "When";
        this._colWhen.Width = 130;
        //
        // _colEmulator
        //
        this._colEmulator.Text = "Emulator";
        this._colEmulator.Width = 110;
        //
        // _colDevice
        //
        this._colDevice.Text = "Device";
        this._colDevice.Width = 120;
        //
        // _colResult
        //
        this._colResult.Text = "Result";
        this._colResult.Width = 230;
        //
        // _colBytes
        //
        this._colBytes.Text = "Transferred";
        this._colBytes.Width = 90;
        //
        // _colDuration
        //
        this._colDuration.Text = "Duration";
        this._colDuration.Width = 70;
        //
        // _detailHeader
        //
        this._detailHeader.Dock = System.Windows.Forms.DockStyle.Top;
        this._detailHeader.Location = new System.Drawing.Point(0, 0);
        this._detailHeader.Name = "_detailHeader";
        this._detailHeader.Size = new System.Drawing.Size(900, 22);
        this._detailHeader.TabIndex = 0;
        this._detailHeader.Text = "Select a sync to see the files it touched.";
        this._detailHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _operations
        //
        this._operations.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this._colAction,
            this._colFile,
            this._colSize,
            this._colTime});
        this._operations.Dock = System.Windows.Forms.DockStyle.Fill;
        this._operations.FullRowSelect = true;
        this._operations.Location = new System.Drawing.Point(0, 22);
        this._operations.Name = "_operations";
        this._operations.Size = new System.Drawing.Size(900, 219);
        this._operations.TabIndex = 1;
        this._operations.UseCompatibleStateImageBehavior = false;
        this._operations.View = System.Windows.Forms.View.Details;
        //
        // _colAction
        //
        this._colAction.Text = "What";
        this._colAction.Width = 140;
        //
        // _colFile
        //
        this._colFile.Text = "File";
        this._colFile.Width = 430;
        //
        // _colSize
        //
        this._colSize.Text = "Size";
        this._colSize.Width = 90;
        //
        // _colTime
        //
        this._colTime.Text = "Time";
        this._colTime.Width = 80;
        //
        // HistoryForm
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(900, 560);
        this.Controls.Add(this._split);
        this.Controls.Add(this._toolbar);
        this.MinimumSize = new System.Drawing.Size(700, 440);
        this.Name = "HistoryForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "EmuSync – Sync history";
        this._toolbar.ResumeLayout(false);
        this._toolbar.PerformLayout();
        this._split.Panel1.ResumeLayout(false);
        this._split.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this._split)).EndInit();
        this._split.ResumeLayout(false);
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion
}
