namespace LocalEnterprise.Application.Identity;

public sealed record TwoFactorEnrollmentDto(
    string SharedSecret,
    string ProvisioningUri,
    bool TwoFactorEnabled);