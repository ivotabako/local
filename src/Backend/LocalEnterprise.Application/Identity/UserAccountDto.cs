namespace LocalEnterprise.Application.Identity;

public sealed record UserAccountDto(
    Guid Id,
    string Username,
    string[] Roles,
    DateTime CreatedAt,
    string? CreatedBy,
    bool RequiresPasswordChange,
    DateTime? LastPasswordChangedAt,
    bool IsLocked,
    bool TwoFactorEnabled,
    int RecoveryCodesRemaining);