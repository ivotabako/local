namespace LocalEnterprise.Application.Abstractions;

public interface IMfaService
{
    string GenerateSharedSecret();
    string BuildProvisioningUri(string issuer, string username, string sharedSecret);
    bool ValidateCode(string sharedSecret, string code);
    string[] GenerateRecoveryCodes(int count);
}