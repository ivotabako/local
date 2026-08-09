using LocalEnterprise.Domain.Identity;

namespace LocalEnterprise.Application.Abstractions;

public interface IUserAccountRepository
{
    Task<IReadOnlyList<UserAccount>> ListAsync(CancellationToken cancellationToken);
    Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<UserAccount?> GetByNormalizedUsernameAsync(string normalizedUsername, CancellationToken cancellationToken);
    Task CreateAsync(UserAccount userAccount, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(UserAccount userAccount, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<long> CountInRoleAsync(string role, CancellationToken cancellationToken);
    Task<bool> AnyAsync(CancellationToken cancellationToken);
}