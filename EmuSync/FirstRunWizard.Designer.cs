namespace EmuSync;

partial class FirstRunWizard
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null!;

    private System.Windows.Forms.Label _title;
    private System.Windows.Forms.Label _body;
    private System.Windows.Forms.Button _btnNext;
    private System.Windows.Forms.Button _btnCancel;

    // One panel per step, all three sharing the same area. Only one is visible at
    // a time; in the designer they overlap, so use the Document Outline window to
    // pick the one you want to edit.
    private System.Windows.Forms.Panel _panelAccount;
    private System.Windows.Forms.Button _btnSignIn;
    private System.Windows.Forms.Label _accountStatus;

    private System.Windows.Forms.Panel _panelDrive;
    private System.Windows.Forms.Button _btnDrive;
    private System.Windows.Forms.Label _driveStatus;

    private System.Windows.Forms.Panel _panelEmulators;
    private System.Windows.Forms.CheckedListBox _emulatorList;
    private System.Windows.Forms.Button _btnAddManual;
    private System.Windows.Forms.Button _btnRedetect;
    private System.Windows.Forms.CheckBox _chkStartup;

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
        this._btnNext = new System.Windows.Forms.Button();
        this._btnCancel = new System.Windows.Forms.Button();
        this._panelAccount = new System.Windows.Forms.Panel();
        this._btnSignIn = new System.Windows.Forms.Button();
        this._accountStatus = new System.Windows.Forms.Label();
        this._panelDrive = new System.Windows.Forms.Panel();
        this._btnDrive = new System.Windows.Forms.Button();
        this._driveStatus = new System.Windows.Forms.Label();
        this._panelEmulators = new System.Windows.Forms.Panel();
        this._emulatorList = new System.Windows.Forms.CheckedListBox();
        this._btnAddManual = new System.Windows.Forms.Button();
        this._btnRedetect = new System.Windows.Forms.Button();
        this._chkStartup = new System.Windows.Forms.CheckBox();
        this._panelAccount.SuspendLayout();
        this._panelDrive.SuspendLayout();
        this._panelEmulators.SuspendLayout();
        this.SuspendLayout();
        //
        // _title
        //
        this._title.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
        this._title.Location = new System.Drawing.Point(20, 15);
        this._title.Name = "_title";
        this._title.Size = new System.Drawing.Size(520, 28);
        this._title.TabIndex = 0;
        this._title.Text = "Welcome to EmuSync!";
        //
        // _body
        //
        this._body.Location = new System.Drawing.Point(20, 48);
        this._body.Name = "_body";
        this._body.Size = new System.Drawing.Size(520, 76);
        this._body.TabIndex = 1;
        //
        // _panelAccount
        //
        this._panelAccount.Controls.Add(this._btnSignIn);
        this._panelAccount.Controls.Add(this._accountStatus);
        this._panelAccount.Location = new System.Drawing.Point(20, 130);
        this._panelAccount.Name = "_panelAccount";
        this._panelAccount.Size = new System.Drawing.Size(520, 190);
        this._panelAccount.TabIndex = 2;
        //
        // _btnSignIn
        //
        this._btnSignIn.Location = new System.Drawing.Point(0, 10);
        this._btnSignIn.Name = "_btnSignIn";
        this._btnSignIn.Size = new System.Drawing.Size(200, 34);
        this._btnSignIn.TabIndex = 0;
        this._btnSignIn.Text = "Sign in / create account";
        this._btnSignIn.UseVisualStyleBackColor = true;
        //
        // _accountStatus
        //
        this._accountStatus.Location = new System.Drawing.Point(212, 16);
        this._accountStatus.Name = "_accountStatus";
        this._accountStatus.Size = new System.Drawing.Size(308, 60);
        this._accountStatus.TabIndex = 1;
        //
        // _panelDrive
        //
        this._panelDrive.Controls.Add(this._btnDrive);
        this._panelDrive.Controls.Add(this._driveStatus);
        this._panelDrive.Location = new System.Drawing.Point(20, 130);
        this._panelDrive.Name = "_panelDrive";
        this._panelDrive.Size = new System.Drawing.Size(520, 190);
        this._panelDrive.TabIndex = 3;
        //
        // _btnDrive
        //
        this._btnDrive.Location = new System.Drawing.Point(0, 10);
        this._btnDrive.Name = "_btnDrive";
        this._btnDrive.Size = new System.Drawing.Size(200, 34);
        this._btnDrive.TabIndex = 0;
        this._btnDrive.Text = "Connect Google Drive";
        this._btnDrive.UseVisualStyleBackColor = true;
        //
        // _driveStatus
        //
        this._driveStatus.Location = new System.Drawing.Point(212, 16);
        this._driveStatus.Name = "_driveStatus";
        this._driveStatus.Size = new System.Drawing.Size(308, 60);
        this._driveStatus.TabIndex = 1;
        //
        // _panelEmulators
        //
        this._panelEmulators.Controls.Add(this._emulatorList);
        this._panelEmulators.Controls.Add(this._btnAddManual);
        this._panelEmulators.Controls.Add(this._btnRedetect);
        this._panelEmulators.Controls.Add(this._chkStartup);
        this._panelEmulators.Location = new System.Drawing.Point(20, 130);
        this._panelEmulators.Name = "_panelEmulators";
        this._panelEmulators.Size = new System.Drawing.Size(520, 190);
        this._panelEmulators.TabIndex = 4;
        //
        // _emulatorList
        //
        this._emulatorList.CheckOnClick = true;
        this._emulatorList.Location = new System.Drawing.Point(0, 4);
        this._emulatorList.Name = "_emulatorList";
        this._emulatorList.Size = new System.Drawing.Size(400, 148);
        this._emulatorList.TabIndex = 0;
        //
        // _btnAddManual
        //
        this._btnAddManual.Location = new System.Drawing.Point(410, 4);
        this._btnAddManual.Name = "_btnAddManual";
        this._btnAddManual.Size = new System.Drawing.Size(110, 30);
        this._btnAddManual.TabIndex = 1;
        this._btnAddManual.Text = "Add manually...";
        this._btnAddManual.UseVisualStyleBackColor = true;
        //
        // _btnRedetect
        //
        this._btnRedetect.Location = new System.Drawing.Point(410, 40);
        this._btnRedetect.Name = "_btnRedetect";
        this._btnRedetect.Size = new System.Drawing.Size(110, 30);
        this._btnRedetect.TabIndex = 2;
        this._btnRedetect.Text = "Detect again";
        this._btnRedetect.UseVisualStyleBackColor = true;
        //
        // _chkStartup
        //
        this._chkStartup.Checked = true;
        this._chkStartup.CheckState = System.Windows.Forms.CheckState.Checked;
        this._chkStartup.Location = new System.Drawing.Point(0, 162);
        this._chkStartup.Name = "_chkStartup";
        this._chkStartup.Size = new System.Drawing.Size(520, 24);
        this._chkStartup.TabIndex = 3;
        this._chkStartup.Text = "Start EmuSync automatically with Windows (in the tray)";
        this._chkStartup.UseVisualStyleBackColor = true;
        //
        // _btnNext
        //
        this._btnNext.Location = new System.Drawing.Point(360, 334);
        this._btnNext.Name = "_btnNext";
        this._btnNext.Size = new System.Drawing.Size(90, 30);
        this._btnNext.TabIndex = 5;
        this._btnNext.Text = "Next >";
        this._btnNext.UseVisualStyleBackColor = true;
        //
        // _btnCancel
        //
        this._btnCancel.Location = new System.Drawing.Point(455, 334);
        this._btnCancel.Name = "_btnCancel";
        this._btnCancel.Size = new System.Drawing.Size(90, 30);
        this._btnCancel.TabIndex = 6;
        this._btnCancel.Text = "Cancel";
        this._btnCancel.UseVisualStyleBackColor = true;
        //
        // FirstRunWizard
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(560, 380);
        this.Controls.Add(this._title);
        this.Controls.Add(this._body);
        this.Controls.Add(this._panelAccount);
        this.Controls.Add(this._panelDrive);
        this.Controls.Add(this._panelEmulators);
        this.Controls.Add(this._btnNext);
        this.Controls.Add(this._btnCancel);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "FirstRunWizard";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "EmuSync – First-run setup";
        this._panelAccount.ResumeLayout(false);
        this._panelDrive.ResumeLayout(false);
        this._panelEmulators.ResumeLayout(false);
        this.ResumeLayout(false);
    }

    #endregion
}
