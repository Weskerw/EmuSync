using EmuSync.Core;

namespace EmuSync;

/// <summary>
/// EmuSync account sign-in (Firebase Authentication).
///
/// Two routes, as chosen in the Firebase console:
///  • "Continue with Google": one browser consent that covers both the EmuSync
///    account and Drive access, so a Google user signs in once.
///  • email + password: the account is created here; Drive is authorized right
///    afterwards, since the saves still live in the user's own Drive.
/// </summary>
public class LoginForm : Form
{
    private readonly EmuSyncServices _services;

    private readonly Label _title = new();
    private readonly Label _subtitle = new();
    private readonly Button _btnGoogle = new();
    private readonly Label _or = new();
    private readonly Label _lblEmail = new();
    private readonly TextBox _email = new();
    private readonly Label _lblPassword = new();
    private readonly TextBox _password = new();
    private readonly Button _btnPrimary = new();
    private readonly LinkLabel _linkToggle = new();
    private readonly LinkLabel _linkForgot = new();
    private readonly Label _status = new();

    private bool _signUpMode;

    public LoginForm(EmuSyncServices services)
    {
        _services = services;

        Text = "EmuSync – Sign in";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(420, 400);

        _title.Text = "Sign in to EmuSync";
        _title.Font = new Font(Font.FontFamily, 13f, FontStyle.Bold);
        _title.SetBounds(24, 20, 380, 30);

        _subtitle.Text = "Your settings live in your EmuSync account; the saves stay in your own Google Drive.";
        _subtitle.SetBounds(24, 50, 372, 40);

        _btnGoogle.Text = "Continue with Google";
        _btnGoogle.SetBounds(24, 98, 372, 36);
        _btnGoogle.Click += async (_, _) => await GoogleSignInAsync();

        _or.Text = "— or use an email address —";
        _or.TextAlign = ContentAlignment.MiddleCenter;
        _or.SetBounds(24, 144, 372, 24);

        _lblEmail.Text = "Email";
        _lblEmail.SetBounds(24, 176, 100, 20);
        _email.SetBounds(24, 196, 372, 24);

        _lblPassword.Text = "Password";
        _lblPassword.SetBounds(24, 228, 100, 20);
        _password.SetBounds(24, 248, 372, 24);
        _password.UseSystemPasswordChar = true;

        _btnPrimary.SetBounds(24, 284, 372, 34);
        _btnPrimary.Click += async (_, _) => await EmailSignInAsync();

        _linkToggle.SetBounds(24, 326, 250, 20);
        _linkToggle.LinkClicked += (_, _) => SetMode(!_signUpMode);

        _linkForgot.Text = "Forgot your password?";
        _linkForgot.TextAlign = ContentAlignment.TopRight;
        _linkForgot.SetBounds(256, 326, 140, 20);
        _linkForgot.LinkClicked += async (_, _) => await ResetPasswordAsync();

        _status.SetBounds(24, 350, 372, 40);
        _status.ForeColor = SystemColors.GrayText;

        Controls.AddRange(new Control[]
        {
            _title, _subtitle, _btnGoogle, _or, _lblEmail, _email, _lblPassword, _password,
            _btnPrimary, _linkToggle, _linkForgot, _status
        });

        AcceptButton = _btnPrimary;
        SetMode(signUp: false);

        if (!_services.Firebase.IsConfigured)
        {
            _btnGoogle.Enabled = _btnPrimary.Enabled = false;
            SetStatus($"Firebase is not configured: add {FirebaseOptions.FileName} next to EmuSync.exe (see the README).",
                error: true);
        }
    }

    private void SetMode(bool signUp)
    {
        _signUpMode = signUp;
        _btnPrimary.Text = signUp ? "Create account" : "Sign in";
        _linkToggle.Text = signUp ? "I already have an account" : "Create a new account";
        _linkForgot.Visible = !signUp;
    }

    private async Task GoogleSignInAsync()
    {
        SetBusy(true, "Waiting for the browser...");
        try
        {
            await _services.SignInWithGoogleAsync();
            Succeed();
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, error: true);
            SetBusy(false);
        }
    }

    private async Task EmailSignInAsync()
    {
        string email = _email.Text.Trim();
        string password = _password.Text;

        if (email.Length == 0 || password.Length == 0)
        {
            SetStatus("Enter your email address and password.", error: true);
            return;
        }

        SetBusy(true, _signUpMode ? "Creating the account..." : "Signing in...");
        try
        {
            if (_signUpMode) await _services.SignUpAsync(email, password);
            else await _services.SignInWithPasswordAsync(email, password);
            Succeed();
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, error: true);
            SetBusy(false);
        }
    }

    private async Task ResetPasswordAsync()
    {
        string email = _email.Text.Trim();
        if (email.Length == 0)
        {
            SetStatus("Enter your email address first, then click again.", error: true);
            return;
        }

        SetBusy(true, "Sending the email...");
        try
        {
            await _services.SendPasswordResetAsync(email);
            SetStatus("Password reset email sent. Check your inbox.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, error: true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void Succeed()
    {
        DialogResult = DialogResult.OK;
        Close();
    }

    private void SetBusy(bool busy, string? message = null)
    {
        _btnGoogle.Enabled = _btnPrimary.Enabled = _linkToggle.Enabled = _linkForgot.Enabled = !busy;
        _email.Enabled = _password.Enabled = !busy;
        UseWaitCursor = busy;
        if (message != null) SetStatus(message);
    }

    private void SetStatus(string message, bool error = false)
    {
        _status.ForeColor = error ? Color.Firebrick : SystemColors.GrayText;
        _status.Text = message;
    }
}
