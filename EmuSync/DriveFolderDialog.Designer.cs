namespace EmuSync;

partial class DriveFolderDialog
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null!;

    private System.Windows.Forms.Label _title;
    private System.Windows.Forms.Label _body;
    private System.Windows.Forms.Label _lblPath;
    private System.Windows.Forms.TextBox _path;
    private System.Windows.Forms.Label _preview;
    private System.Windows.Forms.Label _warning;
    private System.Windows.Forms.Button _ok;
    private System.Windows.Forms.Button _cancel;

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
        this._title = new System.Windows.Forms.Label();
        this._body = new System.Windows.Forms.Label();
        this._lblPath = new System.Windows.Forms.Label();
        this._path = new System.Windows.Forms.TextBox();
        this._preview = new System.Windows.Forms.Label();
        this._warning = new System.Windows.Forms.Label();
        this._ok = new System.Windows.Forms.Button();
        this._cancel = new System.Windows.Forms.Button();
        this.SuspendLayout();
        //
        // _title
        //
        this._title.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
        this._title.Location = new System.Drawing.Point(20, 18);
        this._title.Name = "_title";
        this._title.Size = new System.Drawing.Size(500, 28);
        this._title.TabIndex = 0;
        this._title.Text = "Where to keep the saves on Drive";
        //
        // _body
        //
        this._body.Location = new System.Drawing.Point(20, 50);
        this._body.Name = "_body";
        this._body.Size = new System.Drawing.Size(500, 56);
        this._body.TabIndex = 1;
        this._body.Text = "Folder in your Google Drive, starting from \"My Drive\". Use / for subfolders. Every" +
            " computer follows this setting, so they all keep looking in the same place.";
        //
        // _lblPath
        //
        this._lblPath.Location = new System.Drawing.Point(20, 112);
        this._lblPath.Name = "_lblPath";
        this._lblPath.Size = new System.Drawing.Size(200, 20);
        this._lblPath.TabIndex = 2;
        this._lblPath.Text = "Folder";
        //
        // _path
        //
        this._path.Location = new System.Drawing.Point(20, 132);
        this._path.Name = "_path";
        this._path.Size = new System.Drawing.Size(500, 23);
        this._path.TabIndex = 3;
        //
        // _preview
        //
        this._preview.ForeColor = System.Drawing.SystemColors.GrayText;
        this._preview.Location = new System.Drawing.Point(20, 160);
        this._preview.Name = "_preview";
        this._preview.Size = new System.Drawing.Size(500, 20);
        this._preview.TabIndex = 4;
        //
        // _warning
        //
        this._warning.Location = new System.Drawing.Point(20, 188);
        this._warning.Name = "_warning";
        this._warning.Size = new System.Drawing.Size(500, 56);
        this._warning.TabIndex = 5;
        //
        // _ok
        //
        this._ok.Location = new System.Drawing.Point(354, 252);
        this._ok.Name = "_ok";
        this._ok.Size = new System.Drawing.Size(80, 28);
        this._ok.TabIndex = 6;
        this._ok.Text = "OK";
        this._ok.UseVisualStyleBackColor = true;
        //
        // _cancel
        //
        this._cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this._cancel.Location = new System.Drawing.Point(440, 252);
        this._cancel.Name = "_cancel";
        this._cancel.Size = new System.Drawing.Size(80, 28);
        this._cancel.TabIndex = 7;
        this._cancel.Text = "Cancel";
        this._cancel.UseVisualStyleBackColor = true;
        //
        // DriveFolderDialog
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.CancelButton = this._cancel;
        this.ClientSize = new System.Drawing.Size(540, 292);
        this.Controls.Add(this._title);
        this.Controls.Add(this._body);
        this.Controls.Add(this._lblPath);
        this.Controls.Add(this._path);
        this.Controls.Add(this._preview);
        this.Controls.Add(this._warning);
        this.Controls.Add(this._ok);
        this.Controls.Add(this._cancel);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "DriveFolderDialog";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "EmuSync – Drive folder";
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion
}
