namespace EmuSync;

partial class AddEmulatorDialog
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null!;

    private System.Windows.Forms.Label _lblEmulator;
    private System.Windows.Forms.ComboBox _emulator;
    private System.Windows.Forms.Label _lblCustom;
    private System.Windows.Forms.TextBox _customName;
    private System.Windows.Forms.Label _lblFolder;
    private System.Windows.Forms.TextBox _folder;
    private System.Windows.Forms.Button _browse;
    private System.Windows.Forms.Label _hint;
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
        this._lblEmulator = new System.Windows.Forms.Label();
        this._emulator = new System.Windows.Forms.ComboBox();
        this._lblCustom = new System.Windows.Forms.Label();
        this._customName = new System.Windows.Forms.TextBox();
        this._lblFolder = new System.Windows.Forms.Label();
        this._folder = new System.Windows.Forms.TextBox();
        this._browse = new System.Windows.Forms.Button();
        this._hint = new System.Windows.Forms.Label();
        this._ok = new System.Windows.Forms.Button();
        this._cancel = new System.Windows.Forms.Button();
        this.SuspendLayout();
        //
        // _lblEmulator
        //
        this._lblEmulator.AutoSize = true;
        this._lblEmulator.Location = new System.Drawing.Point(20, 18);
        this._lblEmulator.Name = "_lblEmulator";
        this._lblEmulator.Size = new System.Drawing.Size(200, 20);
        this._lblEmulator.TabIndex = 0;
        this._lblEmulator.Text = "Emulator / console";
        //
        // _emulator
        //
        this._emulator.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._emulator.Location = new System.Drawing.Point(20, 40);
        this._emulator.Name = "_emulator";
        this._emulator.Size = new System.Drawing.Size(480, 23);
        this._emulator.TabIndex = 1;
        //
        // _lblCustom
        //
        this._lblCustom.Location = new System.Drawing.Point(20, 74);
        this._lblCustom.Name = "_lblCustom";
        this._lblCustom.Size = new System.Drawing.Size(100, 20);
        this._lblCustom.TabIndex = 2;
        this._lblCustom.Text = "Name";
        //
        // _customName
        //
        this._customName.Location = new System.Drawing.Point(20, 94);
        this._customName.Name = "_customName";
        this._customName.Size = new System.Drawing.Size(480, 23);
        this._customName.TabIndex = 3;
        //
        // _lblFolder
        //
        this._lblFolder.Location = new System.Drawing.Point(20, 128);
        this._lblFolder.Name = "_lblFolder";
        this._lblFolder.Size = new System.Drawing.Size(200, 20);
        this._lblFolder.TabIndex = 4;
        this._lblFolder.Text = "Local save folder";
        //
        // _folder
        //
        this._folder.Location = new System.Drawing.Point(20, 148);
        this._folder.Name = "_folder";
        this._folder.Size = new System.Drawing.Size(390, 23);
        this._folder.TabIndex = 5;
        //
        // _browse
        //
        this._browse.Location = new System.Drawing.Point(418, 147);
        this._browse.Name = "_browse";
        this._browse.Size = new System.Drawing.Size(82, 26);
        this._browse.TabIndex = 6;
        this._browse.Text = "Browse...";
        this._browse.UseVisualStyleBackColor = true;
        //
        // _hint
        //
        this._hint.ForeColor = System.Drawing.SystemColors.GrayText;
        this._hint.Location = new System.Drawing.Point(20, 178);
        this._hint.Name = "_hint";
        this._hint.Size = new System.Drawing.Size(480, 36);
        this._hint.TabIndex = 7;
        //
        // _ok
        //
        this._ok.Location = new System.Drawing.Point(334, 220);
        this._ok.Name = "_ok";
        this._ok.Size = new System.Drawing.Size(80, 28);
        this._ok.TabIndex = 8;
        this._ok.Text = "OK";
        this._ok.UseVisualStyleBackColor = true;
        //
        // _cancel
        //
        this._cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this._cancel.Location = new System.Drawing.Point(420, 220);
        this._cancel.Name = "_cancel";
        this._cancel.Size = new System.Drawing.Size(80, 28);
        this._cancel.TabIndex = 9;
        this._cancel.Text = "Cancel";
        this._cancel.UseVisualStyleBackColor = true;
        //
        // AddEmulatorDialog
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.CancelButton = this._cancel;
        this.ClientSize = new System.Drawing.Size(520, 260);
        this.Controls.Add(this._lblEmulator);
        this.Controls.Add(this._emulator);
        this.Controls.Add(this._lblCustom);
        this.Controls.Add(this._customName);
        this.Controls.Add(this._lblFolder);
        this.Controls.Add(this._folder);
        this.Controls.Add(this._browse);
        this.Controls.Add(this._hint);
        this.Controls.Add(this._ok);
        this.Controls.Add(this._cancel);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "AddEmulatorDialog";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Add an emulator";
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion
}
