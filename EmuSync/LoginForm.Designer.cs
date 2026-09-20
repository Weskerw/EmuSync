namespace EmuSync;

partial class LoginForm
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null!;

    private System.Windows.Forms.Label _title;
    private System.Windows.Forms.Label _subtitle;
    private System.Windows.Forms.Button _btnGoogle;
    private System.Windows.Forms.Label _or;
    private System.Windows.Forms.Label _lblEmail;
    private System.Windows.Forms.TextBox _email;
    private System.Windows.Forms.Label _lblPassword;
    private System.Windows.Forms.TextBox _password;
    private System.Windows.Forms.Button _btnPrimary;
    private System.Windows.Forms.LinkLabel _linkToggle;
    private System.Windows.Forms.LinkLabel _linkForgot;
    private System.Windows.Forms.Label _status;

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
        this._subtitle = new System.Windows.Forms.Label();
        this._btnGoogle = new System.Windows.Forms.Button();
        this._or = new System.Windows.Forms.Label();
        this._lblEmail = new System.Windows.Forms.Label();
        this._email = new System.Windows.Forms.TextBox();
        this._lblPassword = new System.Windows.Forms.Label();
        this._password = new System.Windows.Forms.TextBox();
        this._btnPrimary = new System.Windows.Forms.Button();
        this._linkToggle = new System.Windows.Forms.LinkLabel();
        this._linkForgot = new System.Windows.Forms.LinkLabel();
        this._status = new System.Windows.Forms.Label();
        this.SuspendLayout();
        //
        // _title
        //
        this._title.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
        this._title.Location = new System.Drawing.Point(24, 20);
        this._title.Name = "_title";
        this._title.Size = new System.Drawing.Size(372, 30);
        this._title.TabIndex = 0;
        this._title.Text = "Sign in to EmuSync";
        //
        // _subtitle
        //
        this._subtitle.Location = new System.Drawing.Point(24, 50);
        this._subtitle.Name = "_subtitle";
        this._subtitle.Size = new System.Drawing.Size(372, 40);
        this._subtitle.TabIndex = 1;
        this._subtitle.Text = "Your settings live in your EmuSync account; the saves stay in your own Google Driv" +
            "e.";
        //
        // _btnGoogle
        //
        this._btnGoogle.Location = new System.Drawing.Point(24, 98);
        this._btnGoogle.Name = "_btnGoogle";
        this._btnGoogle.Size = new System.Drawing.Size(372, 36);
        this._btnGoogle.TabIndex = 2;
        this._btnGoogle.Text = "Continue with Google";
        this._btnGoogle.UseVisualStyleBackColor = true;
        //
        // _or
        //
        this._or.Location = new System.Drawing.Point(24, 144);
        this._or.Name = "_or";
        this._or.Size = new System.Drawing.Size(372, 24);
        this._or.TabIndex = 3;
        this._or.Text = "— or use an email address —";
        this._or.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        //
        // _lblEmail
        //
        this._lblEmail.Location = new System.Drawing.Point(24, 176);
        this._lblEmail.Name = "_lblEmail";
        this._lblEmail.Size = new System.Drawing.Size(100, 20);
        this._lblEmail.TabIndex = 4;
        this._lblEmail.Text = "Email";
        //
        // _email
        //
        this._email.Location = new System.Drawing.Point(24, 196);
        this._email.Name = "_email";
        this._email.Size = new System.Drawing.Size(372, 23);
        this._email.TabIndex = 5;
        //
        // _lblPassword
        //
        this._lblPassword.Location = new System.Drawing.Point(24, 228);
        this._lblPassword.Name = "_lblPassword";
        this._lblPassword.Size = new System.Drawing.Size(100, 20);
        this._lblPassword.TabIndex = 6;
        this._lblPassword.Text = "Password";
        //
        // _password
        //
        this._password.Location = new System.Drawing.Point(24, 248);
        this._password.Name = "_password";
        this._password.Size = new System.Drawing.Size(372, 23);
        this._password.TabIndex = 7;
        this._password.UseSystemPasswordChar = true;
        //
        // _btnPrimary
        //
        this._btnPrimary.Location = new System.Drawing.Point(24, 284);
        this._btnPrimary.Name = "_btnPrimary";
        this._btnPrimary.Size = new System.Drawing.Size(372, 34);
        this._btnPrimary.TabIndex = 8;
        this._btnPrimary.Text = "Sign in";
        this._btnPrimary.UseVisualStyleBackColor = true;
        //
        // _linkToggle
        //
        this._linkToggle.Location = new System.Drawing.Point(24, 326);
        this._linkToggle.Name = "_linkToggle";
        this._linkToggle.Size = new System.Drawing.Size(250, 20);
        this._linkToggle.TabIndex = 9;
        this._linkToggle.TabStop = true;
        this._linkToggle.Text = "Create a new account";
        //
        // _linkForgot
        //
        this._linkForgot.Location = new System.Drawing.Point(256, 326);
        this._linkForgot.Name = "_linkForgot";
        this._linkForgot.Size = new System.Drawing.Size(140, 20);
        this._linkForgot.TabIndex = 10;
        this._linkForgot.TabStop = true;
        this._linkForgot.Text = "Forgot your password?";
        this._linkForgot.TextAlign = System.Drawing.ContentAlignment.TopRight;
        //
        // _status
        //
        this._status.ForeColor = System.Drawing.SystemColors.GrayText;
        this._status.Location = new System.Drawing.Point(24, 350);
        this._status.Name = "_status";
        this._status.Size = new System.Drawing.Size(372, 40);
        this._status.TabIndex = 11;
        //
        // LoginForm
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(420, 400);
        this.Controls.Add(this._title);
        this.Controls.Add(this._subtitle);
        this.Controls.Add(this._btnGoogle);
        this.Controls.Add(this._or);
        this.Controls.Add(this._lblEmail);
        this.Controls.Add(this._email);
        this.Controls.Add(this._lblPassword);
        this.Controls.Add(this._password);
        this.Controls.Add(this._btnPrimary);
        this.Controls.Add(this._linkToggle);
        this.Controls.Add(this._linkForgot);
        this.Controls.Add(this._status);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "LoginForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "EmuSync – Sign in";
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion
}
