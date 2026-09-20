using EmuSync.Core;

namespace EmuSync;

/// <summary>
/// EmuSync account sign-in (Firebase Authentication). The layout lives in
/// LoginForm.Designer.cs so it can be opened in the Visual Studio designer;
/// this file only holds the behaviour.
///
/// Two routes, as chosen in the Firebase console:
///  • "Continue with Google": one browser consent that covers both the EmuSync
///    account and Drive access, so a Google user signs in once.
///  • email + password: the account is created here; Drive is authorized right
///    afterwards, since the saves still live in the user's own Drive.
/// </summary>
public partial class LoginForm : Form
{
    private readonly EmuSyncServices _services;

    private bool _signUpMode;

    /// <summary>Parameterless constructor required by the Visual Studio designer.</summary>
    private LoginForm()
    {
        InitializeComponent();
        _services = null!;
    }

    public LoginForm(EmuSyncServices services)
    {
        InitializeComponent();
        _services = services;

        _btnGoogle.Click += async (_, _) => await GoogleSignInAsync();
        _btnPrimary.Click += async (_, _) => await EmailSignInAsync();
        _linkToggle.LinkClicked += (_, _) => SetMode(!_signUpMode);
        _linkForgot.LinkClicked += async (_, _) => await ResetPasswordAsync();

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
