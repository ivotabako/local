using LocalEnterprise.Application.Abstractions;
using LocalEnterprise.Application.Identity;
using LocalEnterprise.Domain.Identity;

namespace LocalEnterprise.Tests.Unit;

public class UserAccountServiceTests
{
    [Fact]
    public async Task CreateAsync_ReturnsConflict_WhenUsernameAlreadyExists()
    {
        var repository = new InMemoryUserAccountRepository();
        var passwordHashService = new FakePasswordHashService();
        var service = new UserAccountService(repository, passwordHashService, new FakeMfaService());

        await service.CreateAsync(
            new CreateUserAccountRequest("alice", "Password_123!", [AuthRoles.Admin]),
            "seed",
            CancellationToken.None);

        var result = await service.CreateAsync(
            new CreateUserAccountRequest("Alice", "Password_123!", [AuthRoles.Reader]),
            "seed",
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(UserAccountErrors.DuplicateUsername, result.ErrorCode);
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsUser_WhenPasswordMatches()
    {
        var repository = new InMemoryUserAccountRepository();
        var passwordHashService = new FakePasswordHashService();
        var service = new UserAccountService(repository, passwordHashService, new FakeMfaService());

        var created = await service.CreateAsync(
            new CreateUserAccountRequest("alice", "Password_123!", [AuthRoles.Admin]),
            "seed",
            CancellationToken.None);

        var result = await service.AuthenticateAsync("alice", "Password_123!", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(created.User!.Id, result!.Id);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsConflict_WhenDeletingLastAdmin()
    {
        var repository = new InMemoryUserAccountRepository();
        var passwordHashService = new FakePasswordHashService();
        var service = new UserAccountService(repository, passwordHashService, new FakeMfaService());

        var created = await service.CreateAsync(
            new CreateUserAccountRequest("alice", "Password_123!", [AuthRoles.Admin]),
            "seed",
            CancellationToken.None);

        var result = await service.DeleteAsync(created.User!.Id, actorId: null, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(UserAccountErrors.CannotDeleteLastAdmin, result.ErrorCode);
    }

    [Fact]
    public async Task CreateAsync_MarksNewUsersForPasswordChange()
    {
        var repository = new InMemoryUserAccountRepository();
        var passwordHashService = new FakePasswordHashService();
        var service = new UserAccountService(repository, passwordHashService, new FakeMfaService());

        var result = await service.CreateAsync(
            new CreateUserAccountRequest("alice", "Password_1234!", [AuthRoles.Reader]),
            "seed",
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.True(result.User!.RequiresPasswordChange);
        Assert.Null(result.User.LastPasswordChangedAt);
    }

    [Fact]
    public async Task ChangePasswordAsync_ClearsPasswordChangeFlag_WhenCurrentPasswordMatches()
    {
        var repository = new InMemoryUserAccountRepository();
        var passwordHashService = new FakePasswordHashService();
        var service = new UserAccountService(repository, passwordHashService, new FakeMfaService());

        var created = await service.CreateAsync(
            new CreateUserAccountRequest("alice", "Password_1234!", [AuthRoles.Reader]),
            "seed",
            CancellationToken.None);

        var changed = await service.ChangePasswordAsync(
            created.User!.Id,
            new ChangePasswordRequest("Password_1234!", "ChangedPassword_5678!"),
            CancellationToken.None);

        Assert.True(changed.Succeeded);
        Assert.False(changed.User!.RequiresPasswordChange);
        Assert.NotNull(changed.User.LastPasswordChangedAt);
    }

    [Fact]
    public async Task ResetPasswordAsync_SetsTemporaryPassword_AndRequiresPasswordChange()
    {
        var repository = new InMemoryUserAccountRepository();
        var passwordHashService = new FakePasswordHashService();
        var service = new UserAccountService(repository, passwordHashService, new FakeMfaService());

        var created = await service.CreateAsync(
            new CreateUserAccountRequest("alice", "Password_1234!", [AuthRoles.Reader]),
            "seed",
            CancellationToken.None);

        var changed = await service.ChangePasswordAsync(
            created.User!.Id,
            new ChangePasswordRequest("Password_1234!", "ChangedPassword_5678!"),
            CancellationToken.None);

        Assert.True(changed.Succeeded);

        var reset = await service.ResetPasswordAsync(created.User.Id, "TempPassword_9012!", CancellationToken.None);

        Assert.True(reset.Succeeded);
        Assert.True(reset.User!.RequiresPasswordChange);

        var authenticated = await service.AuthenticateAsync("alice", "TempPassword_9012!", CancellationToken.None);
        Assert.NotNull(authenticated);
        Assert.True(authenticated!.RequiresPasswordChange);
    }

    [Fact]
    public async Task SetLockStateAsync_BlocksAuthentication_WhenLocked()
    {
        var repository = new InMemoryUserAccountRepository();
        var passwordHashService = new FakePasswordHashService();
        var service = new UserAccountService(repository, passwordHashService, new FakeMfaService());

        var created = await service.CreateAsync(
            new CreateUserAccountRequest("alice", "Password_1234!", [AuthRoles.Reader]),
            "seed",
            CancellationToken.None);

        var locked = await service.SetLockStateAsync(created.User!.Id, true, CancellationToken.None);

        Assert.True(locked.Succeeded);
        Assert.True(locked.User!.IsLocked);
        Assert.Null(await service.AuthenticateAsync("alice", "Password_1234!", CancellationToken.None));

        var unlocked = await service.SetLockStateAsync(created.User.Id, false, CancellationToken.None);

        Assert.True(unlocked.Succeeded);
        Assert.False(unlocked.User!.IsLocked);
        Assert.NotNull(await service.AuthenticateAsync("alice", "Password_1234!", CancellationToken.None));
    }

    [Fact]
    public async Task BeginAndEnableTwoFactorAsync_StoresRecoveryCodes_AndEnablesTwoFactor()
    {
        var repository = new InMemoryUserAccountRepository();
        var passwordHashService = new FakePasswordHashService();
        var mfaService = new FakeMfaService();
        var service = new UserAccountService(repository, passwordHashService, mfaService);

        var created = await service.CreateAsync(
            new CreateUserAccountRequest("alice", "Password_1234!", [AuthRoles.Reader]),
            "seed",
            CancellationToken.None);

        var enrollment = await service.BeginTwoFactorEnrollmentAsync(created.User!.Id, "LocalEnterprise", CancellationToken.None);

        Assert.True(enrollment.Succeeded);
        Assert.Equal("SECRET-1", enrollment.SharedSecret);

        var enabled = await service.EnableTwoFactorAsync(created.User.Id, "123456", CancellationToken.None);

        Assert.True(enabled.Succeeded);
        Assert.True(enabled.User!.TwoFactorEnabled);
        Assert.Equal(8, enabled.RecoveryCodes.Length);
        Assert.Equal(8, enabled.User.RecoveryCodesRemaining);
    }

    [Fact]
    public async Task AuthenticateAsync_LocksUser_AfterFiveFailedAttempts()
    {
        var repository = new InMemoryUserAccountRepository();
        var passwordHashService = new FakePasswordHashService();
        var service = new UserAccountService(repository, passwordHashService, new FakeMfaService());

        var created = await service.CreateAsync(
            new CreateUserAccountRequest("alice", "Password_1234!", [AuthRoles.Reader]),
            "seed",
            CancellationToken.None);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var result = await service.AuthenticateAsync("alice", "WrongPassword_1234!", CancellationToken.None);
            Assert.Null(result);
        }

        var lockedUser = await service.GetByIdAsync(created.User!.Id, CancellationToken.None);
        Assert.NotNull(lockedUser);
        Assert.True(lockedUser!.IsLocked);
    }

    [Fact]
    public async Task RegenerateRecoveryCodesAsync_ReplacesExistingCodes()
    {
        var repository = new InMemoryUserAccountRepository();
        var passwordHashService = new FakePasswordHashService();
        var mfaService = new FakeMfaService();
        var service = new UserAccountService(repository, passwordHashService, mfaService);

        var created = await service.CreateAsync(
            new CreateUserAccountRequest("alice", "Password_1234!", [AuthRoles.Reader]),
            "seed",
            CancellationToken.None);

        await service.BeginTwoFactorEnrollmentAsync(created.User!.Id, "LocalEnterprise", CancellationToken.None);
        var enabled = await service.EnableTwoFactorAsync(created.User.Id, "123456", CancellationToken.None);
        Assert.True(enabled.Succeeded);

        var regenerated = await service.RegenerateRecoveryCodesAsync(created.User.Id, "123456", CancellationToken.None);

        Assert.True(regenerated.Succeeded);
        Assert.Equal(8, regenerated.RecoveryCodes.Length);
        Assert.Equal(8, regenerated.User!.RecoveryCodesRemaining);
        Assert.NotEqual(enabled.RecoveryCodes[0], regenerated.RecoveryCodes[0]);
    }

    [Fact]
    public async Task DisableTwoFactorAsync_ClearsTwoFactorState()
    {
        var repository = new InMemoryUserAccountRepository();
        var passwordHashService = new FakePasswordHashService();
        var mfaService = new FakeMfaService();
        var service = new UserAccountService(repository, passwordHashService, mfaService);

        var created = await service.CreateAsync(
            new CreateUserAccountRequest("alice", "Password_1234!", [AuthRoles.Reader]),
            "seed",
            CancellationToken.None);

        await service.BeginTwoFactorEnrollmentAsync(created.User!.Id, "LocalEnterprise", CancellationToken.None);
        var enabled = await service.EnableTwoFactorAsync(created.User.Id, "123456", CancellationToken.None);
        Assert.True(enabled.Succeeded);

        var disabled = await service.DisableTwoFactorAsync(created.User.Id, "123456", CancellationToken.None);

        Assert.True(disabled.Succeeded);
        Assert.False(disabled.User!.TwoFactorEnabled);
        Assert.Equal(0, disabled.User.RecoveryCodesRemaining);
    }

    [Fact]
    public async Task ResetTwoFactorAsync_ClearsAdminManagedTwoFactorState()
    {
        var repository = new InMemoryUserAccountRepository();
        var passwordHashService = new FakePasswordHashService();
        var mfaService = new FakeMfaService();
        var service = new UserAccountService(repository, passwordHashService, mfaService);

        var created = await service.CreateAsync(
            new CreateUserAccountRequest("alice", "Password_1234!", [AuthRoles.Reader]),
            "seed",
            CancellationToken.None);

        await service.BeginTwoFactorEnrollmentAsync(created.User!.Id, "LocalEnterprise", CancellationToken.None);
        var enabled = await service.EnableTwoFactorAsync(created.User.Id, "123456", CancellationToken.None);
        Assert.True(enabled.Succeeded);

        var reset = await service.ResetTwoFactorAsync(created.User.Id, CancellationToken.None);

        Assert.True(reset.Succeeded);
        Assert.False(reset.User!.TwoFactorEnabled);
        Assert.Equal(0, reset.User.RecoveryCodesRemaining);
    }

    private sealed class InMemoryUserAccountRepository : IUserAccountRepository
    {
        private readonly List<UserAccount> _users = [];

        public Task<IReadOnlyList<UserAccount>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<UserAccount>>(_users.OrderBy(x => x.Username).ToList());
        }

        public Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(_users.FirstOrDefault(x => x.Id == id));
        }

        public Task<UserAccount?> GetByNormalizedUsernameAsync(string normalizedUsername, CancellationToken cancellationToken)
        {
            return Task.FromResult(_users.FirstOrDefault(x => x.NormalizedUsername == normalizedUsername));
        }

        public Task CreateAsync(UserAccount userAccount, CancellationToken cancellationToken)
        {
            _users.Add(userAccount);
            return Task.CompletedTask;
        }

        public Task<bool> UpdateAsync(UserAccount userAccount, CancellationToken cancellationToken)
        {
            var index = _users.FindIndex(x => x.Id == userAccount.Id);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            _users[index] = userAccount;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            var removed = _users.RemoveAll(x => x.Id == id) > 0;
            return Task.FromResult(removed);
        }

        public Task<long> CountInRoleAsync(string role, CancellationToken cancellationToken)
        {
            var count = _users.LongCount(x => x.Roles.Contains(role, StringComparer.OrdinalIgnoreCase));
            return Task.FromResult(count);
        }

        public Task<bool> AnyAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_users.Count > 0);
        }
    }

    private sealed class FakePasswordHashService : IPasswordHashService
    {
        public string HashPassword(string username, string password)
        {
            return $"HASH::{username}::{password}";
        }

        public bool VerifyHashedPassword(string username, string hashedPassword, string providedPassword)
        {
            return string.Equals(hashedPassword, HashPassword(username, providedPassword), StringComparison.Ordinal);
        }
    }

    private sealed class FakeMfaService : IMfaService
    {
        private int _secretCounter;
        private int _recoveryBatch;

        public string GenerateSharedSecret()
        {
            _secretCounter++;
            return $"SECRET-{_secretCounter}";
        }

        public string BuildProvisioningUri(string issuer, string username, string sharedSecret)
        {
            return $"otpauth://totp/{issuer}:{username}?secret={sharedSecret}";
        }

        public bool ValidateCode(string sharedSecret, string code)
        {
            return string.Equals(code, "123456", StringComparison.Ordinal);
        }

        public string[] GenerateRecoveryCodes(int count)
        {
            _recoveryBatch++;
            return Enumerable.Range(1, count).Select(index => $"batch{_recoveryBatch}-code-{index:0000}").ToArray();
        }
    }
}