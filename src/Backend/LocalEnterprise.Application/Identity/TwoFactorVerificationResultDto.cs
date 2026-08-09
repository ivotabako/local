namespace LocalEnterprise.Application.Identity;

public sealed record TwoFactorVerificationResultDto(
    UserAccountDto User,
    string[] RecoveryCodes);