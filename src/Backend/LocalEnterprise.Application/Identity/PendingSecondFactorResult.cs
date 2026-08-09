namespace LocalEnterprise.Application.Identity;

public sealed record PendingSecondFactorResult(
    bool Succeeded,
    string? ErrorCode,
    string? Error,
    UserAccountDto? User);