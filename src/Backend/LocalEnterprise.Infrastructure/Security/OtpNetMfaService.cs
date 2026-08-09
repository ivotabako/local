using LocalEnterprise.Application.Abstractions;
using OtpNet;
using System.Security.Cryptography;

namespace LocalEnterprise.Infrastructure.Security;

public sealed class OtpNetMfaService : IMfaService
{
    public string GenerateSharedSecret()
    {
        return Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));
    }

    public string BuildProvisioningUri(string issuer, string username, string sharedSecret)
    {
        return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(username)}?secret={sharedSecret}&issuer={Uri.EscapeDataString(issuer)}&digits=6";
    }

    public bool ValidateCode(string sharedSecret, string code)
    {
        if (string.IsNullOrWhiteSpace(sharedSecret) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var totp = new Totp(Base32Encoding.ToBytes(sharedSecret));
        return totp.VerifyTotp(code.Trim(), out _, VerificationWindow.RfcSpecifiedNetworkDelay);
    }

    public string[] GenerateRecoveryCodes(int count)
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var codes = new string[count];
        var bytes = new byte[8];

        for (var index = 0; index < count; index++)
        {
            RandomNumberGenerator.Fill(bytes);
            var chars = new char[8];
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = alphabet[bytes[i] % alphabet.Length];
            }

            codes[index] = new string(chars);
        }

        return codes;
    }
}