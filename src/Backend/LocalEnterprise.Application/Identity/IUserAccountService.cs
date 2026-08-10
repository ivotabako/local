namespace LocalEnterprise.Application.Identity;

public interface IUserAccountService
{
    Task<IReadOnlyList<UserAccountDto>> ListAsync(CancellationToken cancellationToken);
    Task<UserAccountDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<UserAccountDto?> GetByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<UserAccountDto?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken);
    Task<(bool Succeeded, string? ErrorCode, string? Error, UserAccountDto? User)> CreateAsync(
        CreateUserAccountRequest request,
        string? createdBy,
        CancellationToken cancellationToken);
    Task<(bool Succeeded, string? ErrorCode, string? Error, UserAccountDto? User)> UpdateAsync(
        Guid id,
        UpdateUserAccountRequest request,
        CancellationToken cancellationToken);
    Task<(bool Succeeded, string? ErrorCode, string? Error, UserAccountDto? User)> ResetPasswordAsync(
        Guid id,
        string newPassword,
        CancellationToken cancellationToken);
    Task<(bool Succeeded, string? ErrorCode, string? Error, UserAccountDto? User)> SetLockStateAsync(
        Guid id,
        bool isLocked,
        CancellationToken cancellationToken);
    Task<(bool Succeeded, string? ErrorCode, string? Error, UserAccountDto? User)> ChangePasswordAsync(
        Guid id,
        ChangePasswordRequest request,
        CancellationToken cancellationToken);
    Task<(bool Succeeded, string? ErrorCode, string? Error, string SharedSecret, string ProvisioningUri)> BeginTwoFactorEnrollmentAsync(
        Guid id,
        string issuer,
        CancellationToken cancellationToken);
    Task<(bool Succeeded, string? ErrorCode, string? Error, UserAccountDto? User, string[] RecoveryCodes)> EnableTwoFactorAsync(
        Guid id,
        string code,
        CancellationToken cancellationToken);
    Task<(bool Succeeded, string? ErrorCode, string? Error, UserAccountDto? User, string[] RecoveryCodes)> RegenerateRecoveryCodesAsync(
        Guid id,
        string code,
        CancellationToken cancellationToken);
    Task<(bool Succeeded, string? ErrorCode, string? Error, UserAccountDto? User)> DisableTwoFactorAsync(
        Guid id,
        string code,
        CancellationToken cancellationToken);
    Task<(bool Succeeded, string? ErrorCode, string? Error, UserAccountDto? User)> ResetTwoFactorAsync(
        Guid id,
        CancellationToken cancellationToken);
    Task<PendingSecondFactorResult> CompleteTwoFactorSignInAsync(
        Guid id,
        string code,
        CancellationToken cancellationToken);
    Task<(bool Succeeded, string? ErrorCode, string? Error)> DeleteAsync(
        Guid id,
        Guid? actorId,
        CancellationToken cancellationToken);
    Task EnsureBootstrapAdminAsync(string username, string password, IReadOnlyCollection<string> roles, CancellationToken cancellationToken);
}