using LocalEnterprise.Domain.Abstractions;

namespace LocalEnterprise.Domain.Identity;

public sealed class UserAccount : Entity, IAggregateRoot
{
    public string Username { get; private set; } = string.Empty;
    public string NormalizedUsername { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; private set; } = [];
    public DateTime CreatedAt { get; private set; }
    public string? CreatedBy { get; private set; }
    public bool RequiresPasswordChange { get; private set; } = true;
    public DateTime? LastPasswordChangedAt { get; private set; }
    public bool IsLocked { get; private set; }
    public DateTime? LockedAt { get; private set; }
    public string? TwoFactorSharedSecret { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public IReadOnlyList<string> RecoveryCodeHashes { get; private set; } = [];

    public static UserAccount Create(string username, IEnumerable<string> roles, string? createdBy, bool requiresPasswordChange = true)
    {
        var account = new UserAccount
        {
            Username = NormalizeUsername(username),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = NormalizeOptional(createdBy),
            RequiresPasswordChange = requiresPasswordChange
        };

        account.NormalizedUsername = account.Username.ToUpperInvariant();
        account.Roles = NormalizeRoles(roles);
        return account;
    }

    public static UserAccount Rehydrate(
        Guid id,
        string username,
        string passwordHash,
        IEnumerable<string> roles,
        DateTime createdAt,
        string? createdBy,
        bool requiresPasswordChange,
        DateTime? lastPasswordChangedAt,
        bool isLocked,
        DateTime? lockedAt,
        string? twoFactorSharedSecret,
        bool twoFactorEnabled,
        IEnumerable<string>? recoveryCodeHashes)
    {
        var account = new UserAccount
        {
            Id = id,
            Username = NormalizeUsername(username),
            CreatedAt = createdAt,
            CreatedBy = NormalizeOptional(createdBy),
            RequiresPasswordChange = requiresPasswordChange,
            LastPasswordChangedAt = lastPasswordChangedAt,
            IsLocked = isLocked,
            LockedAt = lockedAt,
            TwoFactorSharedSecret = NormalizeOptional(twoFactorSharedSecret),
            TwoFactorEnabled = twoFactorEnabled,
            RecoveryCodeHashes = recoveryCodeHashes?.ToArray() ?? []
        };

        account.NormalizedUsername = account.Username.ToUpperInvariant();
        account.Roles = NormalizeRoles(roles);
        account.SetPasswordHash(passwordHash);
        return account;
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        PasswordHash = passwordHash.Trim();
    }

    public void UpdateRoles(IEnumerable<string> roles)
    {
        Roles = NormalizeRoles(roles);
    }

    public void RequirePasswordChange()
    {
        RequiresPasswordChange = true;
    }

    public void MarkPasswordChanged(DateTime changedAtUtc)
    {
        RequiresPasswordChange = false;
        LastPasswordChangedAt = changedAtUtc;
    }

    public void Lock(DateTime lockedAtUtc)
    {
        IsLocked = true;
        LockedAt = lockedAtUtc;
    }

    public void Unlock()
    {
        IsLocked = false;
        LockedAt = null;
    }

    public void SetTwoFactorSharedSecret(string sharedSecret)
    {
        if (string.IsNullOrWhiteSpace(sharedSecret))
        {
            throw new ArgumentException("Shared secret is required.", nameof(sharedSecret));
        }

        TwoFactorSharedSecret = sharedSecret.Trim();
    }

    public void EnableTwoFactor(IEnumerable<string> recoveryCodeHashes)
    {
        if (string.IsNullOrWhiteSpace(TwoFactorSharedSecret))
        {
            throw new InvalidOperationException("Two-factor setup has not been started.");
        }

        RecoveryCodeHashes = recoveryCodeHashes?.ToArray() ?? [];
        TwoFactorEnabled = true;
    }

    public void ReplaceRecoveryCodes(IEnumerable<string> recoveryCodeHashes)
    {
        RecoveryCodeHashes = recoveryCodeHashes?.ToArray() ?? [];
    }

    public void DisableTwoFactor()
    {
        TwoFactorEnabled = false;
        TwoFactorSharedSecret = null;
        RecoveryCodeHashes = [];
    }

    private static string NormalizeUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username is required.", nameof(username));
        }

        return username.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static IReadOnlyList<string> NormalizeRoles(IEnumerable<string> roles)
    {
        var normalized = roles?
            .Select(AuthRoles.Normalize)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];

        if (normalized.Length == 0)
        {
            throw new ArgumentException("At least one role is required.", nameof(roles));
        }

        return normalized;
    }
}