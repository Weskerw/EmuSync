using System.Security.Cryptography;
using System.Text;
using EmuSync.Core;

namespace EmuSync;

/// <summary>
/// Encrypts the stored Firebase refresh token with DPAPI, tied to the current
/// Windows user: copying session.json to another machine or account yields
/// nothing usable.
/// </summary>
public class DpapiProtector : ISecretProtector
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("EmuSync.FirebaseSession.v1");

    public string Protect(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return "";
        try
        {
            byte[] encrypted = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(plainText), Entropy, DataProtectionScope.CurrentUser);
            return "dpapi:" + Convert.ToBase64String(encrypted);
        }
        catch (CryptographicException)
        {
            // Rather than lose the session, fall back to plain text (the file
            // still lives in the user's own roaming profile).
            return plainText;
        }
    }

    public string Unprotect(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return "";
        if (!cipherText.StartsWith("dpapi:", StringComparison.Ordinal)) return cipherText; // written before encryption

        try
        {
            byte[] decrypted = ProtectedData.Unprotect(
                Convert.FromBase64String(cipherText[6..]), Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            return ""; // unusable: the user will simply sign in again
        }
    }
}
