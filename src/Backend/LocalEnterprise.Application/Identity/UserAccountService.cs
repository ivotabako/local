using LocalEnterprise.Application.Abstractions;
using LocalEnterprise.Domain.Identity;
using System.Text.RegularExpressions;

namespace LocalEnterprise.Application.Identity;

public sealed class UserAccountService : IUserAccountService
{
    private readonly IUserAccountRepository _repository;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IMfaService _mfaService;

    public UserAccountService(IUserAccountRepository repository, IPasswordHashService passwordHashService, IMfaService mfaService)
    {
        _repository = repository;
        _passwordHashService = passwordHashService;
        _mfaService = mfaService;
    }

    public async Task<IReadOnlyList<UserAccountDto>> ListAsync(CancellationToken cancellationToken)
    {
        var users = await _repository.ListAsync(cancellationToken);
        return users.Select(Map).ToList();
    }

    public async Task<UserAccountDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        return user is null ? null : Map(user);
    }

    public async Task<UserAccountDto?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        var user = await _repository.GetByNormalizedUsernameAsync(username.Trim().ToUpperInvariant(), cancellationToken);
        return user is null ? null : Map(user);
    }

    public async Task<UserAccountDto?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var user = await _repository.GetByNormalizedUsernameAsync(username.Trim().ToUpperInvariant(), cancellationToken);
        if (user is null)
        {
            return null;
        }

        if (user.IsLocked)
        {
            return null;
        }

        var verified = _passwordHashService.VerifyHashedPassword(user.Username, user.PasswordHash, password);
        return verified ? Map(user) : null;
    }

    public async Task<(bool Succeeded, string? ErrorCode, string? Error, UserAccountDto? User)> CreateAsync(
        CreateUserAccountRequest request,
        string? createdBy,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return (false, UserAccountErrors.InvalidUsername, "Username is required.", null);
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return (false, UserAccountErrors.InvalidPassword, "Password is required.", null);
        }

        var passwordError = ValidatePassword(request.Password);
        if (passwordError is not null)
        {
            return (false, UserAccountErrors.WeakPassword, passwordError, null);
        }

        try
        {
            var existing = await _repository.GetByNormalizedUsernameAsync(request.Username.Trim().ToUpperInvariant(), cancellationToken);
            if (existing is not null)
            {
                return (false, UserAccountErrors.DuplicateUsername, "A user with this username already exists.", null);
            }

            var user = UserAccount.Create(request.Username, request.Roles, createdBy, requiresPasswordChange: true);
            user.SetPasswordHash(_passwordHashService.HashPassword(user.Username, request.Password));
            await _repository.CreateAsync(user, cancellationToken);
            return (true, null, null, Map(user));
        }
        catch (ArgumentException ex)
        {
            return (false, UserAccountErrors.InvalidRoles, ex.Message, null);
        }
    }

    public async Task<(bool Succeeded, string? ErrorCode, string? Error, UserAccountDto? User)> ResetPasswordAsync(
        Guid id,
        string newPassword,
        CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return (false, UserAccountErrors.UserNotFound, "User not found.", null);
        }

        var passwordError = ValidatePassword(newPassword);
        if (passwordError is not null)
        {
            return (false, UserAccountErrors.WeakPassword, passwordError, null);
        }

        user.SetPasswordHash(_passwordHashService.HashPassword(user.Username, newPassword));
        user.RequirePasswordChange();
        user.Unlock();

        var updated = await _repository.UpdateAsync(user, cancellationToken);
        return updated
            ? (true, null, null, Map(user))
            : (false, UserAccountErrors.UserNotFound, "User not found.", null);
    }

    public async Task<(bool Succeeded, string? ErrorCode, string? Error, UserAccountDto? User)> SetLockStateAsync(
        Guid id,
        bool isLocked,
        CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return (false, UserAccountErrors.UserNotFound, "User not found.", null);
        }

        if (isLocked)
        {
            user.Lock(DateTime.UtcNow);
        }
        else
        {
            user.Unlock();
        }

        var updated = await _repository.UpdateAsync(user, cancellationToken);
        return updated
            ? (true, null, null, Map(user))
            : (false, UserAccountErrors.UserNotFound, "User not found.", null);
    }

    public async Task<(bool Succeeded, string? ErrorCode, string? Error, UserAccountDto? User)> ChangePasswordAsync(
        Guid id,
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return (false, UserAccountErrors.UserNotFound, "User not found.", null);
        }

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            return (false, UserAccountErrors.InvalidPassword, "Current password is required.", null);
        }

        if (!_passwordHashService.VerifyHashedPassword(user.Username, user.PasswordHash, request.CurrentPassword))
        {
            return (false, UserAccountErrors.IncorrectCurrentPassword, "Current password is incorrect.", null);
        }

        var passwordError = ValidatePassword(request.NewPassword);
        if (passwordError is not null)
        {
            return (false, UserAccountErrors.WeakPassword, passwordError, null);
        }

        if (string.Equals(request.CurrentPassword, request.NewPassword, StringComparison.Ordinal))
        {
            return (false, UserAccountErrors.WeakPassword, "New password must be different from the current password.", null);
        }

        user.SetPasswordHash(_passwordHashService.HashPassword(user.Username, request.NewPassword));
        user.MarkPasswordChanged(DateTime.UtcNow);
        user.Unlock();

        var updated = await _repository.UpdateAsync(user, cancellationToken);
        return updated
            ? (true, null, null, Map(user))
            : (false, UserAccountErrors.UserNotFound, "User not found.", null);
    }

    public async Task<(bool Succeeded, string? ErrorCode, string? Error, string SharedSecret, string ProvisioningUri)> BeginTwoFactorEnrollmentAsync(
        Guid id,
        string issuer,
        CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return (false, UserAccountErrors.UserNotFound, "User not found.", string.Empty, string.Empty);
        }

        if (string.IsNullOrWhiteSpace(user.TwoFactorSharedSecret))
        {
            user.SetTwoFactorSharedSecret(_mfaService.GenerateSharedSecret());
            var updated = await _repository.UpdateAsync(user, cancellationToken);
            if (!updated)
            {
                return (false, UserAccountErrors.UserNotFound, "User not found.", string.Empty, string.Empty);
            }
        }

        var provisioningUri = _mfaService.BuildProvisioningUri(issuer, user.Username, user.TwoFactorSharedSecret!);
        return (true, null, null, user.TwoFactorSharedSecret!, provisioningUri);
    }

    public async Task<(bool Succeeded, string? ErrorCode, string? Error, UserAccountDto? User, string[] RecoveryCodes)> EnableTwoFactorAsync(
        Guid id,
        string code,
        CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return (false, UserAccountErrors.UserNotFound, "User not found.", null, []);
        }

        if (string.IsNullOrWhiteSpace(user.TwoFactorSharedSecret))
        {
            return (false, UserAccountErrors.TwoFactorNotConfigured, "Two-factor setup has not been started.", null, []);
        }

        if (!_mfaService.ValidateCode(user.TwoFactorSharedSecret, code))
        {
            return (false, UserAccountErrors.InvalidTwoFactorCode, "The verification code is invalid.", null, []);
        }

        var recoveryCodes = _mfaService.GenerateRecoveryCodes(8);
        user.EnableTwoFactor(recoveryCodes.Select(recoveryCode => _passwordHashService.HashPassword($"{user.Username}:recovery", recoveryCode)));

        var updated = await _repository.UpdateAsync(user, cancellationToken);
        return updated
            ? (true, null, null, Map(user), recoveryCodes)
            : (false, UserAccountErrors.UserNotFound, "User not found.", null, []);
    }

    public async Task<PendingSecondFactorResult> CompleteTwoFactorSignInAsync(
        Guid id,
        string code,
        CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return new PendingSecondFactorResult(false, UserAccountErrors.UserNotFound, "User not found.", null);
        }

        if (!user.TwoFactorEnabled || string.IsNullOrWhiteSpace(user.TwoFactorSharedSecret))
        {
            return new PendingSecondFactorResult(false, UserAccountErrors.TwoFactorNotConfigured, "Two-factor authentication is not enabled.", null);
        }

        if (_mfaService.ValidateCode(user.TwoFactorSharedSecret, code))
        {
            return new PendingSecondFactorResult(true, null, null, Map(user));
        }

        var remainingRecoveryCodes = user.RecoveryCodeHashes.ToList();
        var matchedIndex = remainingRecoveryCodes.FindIndex(hash =>
            _passwordHashService.VerifyHashedPassword($"{user.Username}:recovery", hash, code));

        if (matchedIndex < 0)
        {
            return new PendingSecondFactorResult(false, UserAccountErrors.InvalidTwoFactorCode, "The verification code is invalid.", null);
        }

        remainingRecoveryCodes.RemoveAt(matchedIndex);
        user.ReplaceRecoveryCodes(remainingRecoveryCodes);
        await _repository.UpdateAsync(user, cancellationToken);
        return new PendingSecondFactorResult(true, null, null, Map(user));
    }

    public async Task<(bool Succeeded, string? ErrorCode, string? Error, UserAccountDto? User)> UpdateAsync(
        Guid id,
        UpdateUserAccountRequest request,
        CancellationToken cancellationToken)
    {
        var current = await _repository.GetByIdAsync(id, cancellationToken);
        if (current is null)
        {
            return (false, UserAccountErrors.UserNotFound, "User not found.", null);
        }

        try
        {
            var isAdminToday = current.Roles.Contains(AuthRoles.Admin, StringComparer.OrdinalIgnoreCase);
            var keepsAdminRole = request.Roles.Any(x => string.Equals(x, AuthRoles.Admin, StringComparison.OrdinalIgnoreCase));
            if (isAdminToday && !keepsAdminRole)
            {
                var adminCount = await _repository.CountInRoleAsync(AuthRoles.Admin, cancellationToken);
                if (adminCount <= 1)
                {
                    return (false, UserAccountErrors.CannotRemoveLastAdminRole, "Cannot remove the last admin role.", null);
                }
            }

            current.UpdateRoles(request.Roles);
            var updated = await _repository.UpdateAsync(current, cancellationToken);
            return updated
                ? (true, null, null, Map(current))
                : (false, UserAccountErrors.UserNotFound, "User not found.", null);
        }
        catch (ArgumentException ex)
        {
            return (false, UserAccountErrors.InvalidRoles, ex.Message, null);
        }
    }

    public async Task<(bool Succeeded, string? ErrorCode, string? Error)> DeleteAsync(
        Guid id,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var current = await _repository.GetByIdAsync(id, cancellationToken);
        if (current is null)
        {
            return (false, UserAccountErrors.UserNotFound, "User not found.");
        }

        if (actorId.HasValue && actorId.Value == id)
        {
            return (false, UserAccountErrors.CannotDeleteCurrentUser, "You cannot delete your own account.");
        }

        if (current.Roles.Contains(AuthRoles.Admin, StringComparer.OrdinalIgnoreCase))
        {
            var adminCount = await _repository.CountInRoleAsync(AuthRoles.Admin, cancellationToken);
            if (adminCount <= 1)
            {
                return (false, UserAccountErrors.CannotDeleteLastAdmin, "Cannot delete the last admin user.");
            }
        }

        var deleted = await _repository.DeleteAsync(id, cancellationToken);
        return deleted
            ? (true, null, null)
            : (false, UserAccountErrors.UserNotFound, "User not found.");
    }

    public async Task EnsureBootstrapAdminAsync(string username, string password, IReadOnlyCollection<string> roles, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByNormalizedUsernameAsync(username.Trim().ToUpperInvariant(), cancellationToken);
        if (existing is not null)
        {
            return;
        }

        var user = UserAccount.Create(username, roles, createdBy: "Bootstrap", requiresPasswordChange: false);
        user.SetPasswordHash(_passwordHashService.HashPassword(user.Username, password));
        user.MarkPasswordChanged(DateTime.UtcNow);
        await _repository.CreateAsync(user, cancellationToken);
    }

    private static UserAccountDto Map(UserAccount user)
    {
        return new UserAccountDto(
            user.Id,
            user.Username,
            user.Roles.ToArray(),
            user.CreatedAt,
            user.CreatedBy,
            user.RequiresPasswordChange,
            user.LastPasswordChangedAt,
            user.IsLocked,
            user.TwoFactorEnabled,
            user.RecoveryCodeHashes.Count);
    }

    private static string? ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return "Password is required.";
        }

        if (password.Length < 12)
        {
            return "Password must be at least 12 characters long.";
        }

        if (!Regex.IsMatch(password, "[A-Z]"))
        {
            return "Password must contain at least one uppercase letter.";
        }

        if (!Regex.IsMatch(password, "[a-z]"))
        {
            return "Password must contain at least one lowercase letter.";
        }

        if (!Regex.IsMatch(password, "[0-9]"))
        {
            return "Password must contain at least one number.";
        }

        if (!Regex.IsMatch(password, "[^a-zA-Z0-9]"))
        {
            return "Password must contain at least one symbol.";
        }

        return null;
    }
}