using LocalEnterprise.Application.Abstractions;
using LocalEnterprise.Domain.Identity;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace LocalEnterprise.Infrastructure.Persistence.Identity;

public sealed class MongoUserAccountRepository : IUserAccountRepository
{
    private readonly IMongoCollection<UserAccountDocument> _users;

    public MongoUserAccountRepository(IMongoDatabase database)
    {
        _users = database.GetCollection<UserAccountDocument>("auth_users");

        var usernameIndex = new CreateIndexModel<UserAccountDocument>(
            Builders<UserAccountDocument>.IndexKeys.Ascending(x => x.NormalizedUsername),
            new CreateIndexOptions { Unique = true, Name = "ux_auth_users_normalized_username" });

        _users.Indexes.CreateOne(usernameIndex);
    }

    public async Task<IReadOnlyList<UserAccount>> ListAsync(CancellationToken cancellationToken)
    {
        var docs = await _users.Find(Builders<UserAccountDocument>.Filter.Empty)
            .SortBy(x => x.Username)
            .ToListAsync(cancellationToken);

        return docs.Select(MapToDomain).ToList();
    }

    public async Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var doc = await _users.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);
        return doc is null ? null : MapToDomain(doc);
    }

    public async Task<UserAccount?> GetByNormalizedUsernameAsync(string normalizedUsername, CancellationToken cancellationToken)
    {
        var doc = await _users.Find(x => x.NormalizedUsername == normalizedUsername).FirstOrDefaultAsync(cancellationToken);
        return doc is null ? null : MapToDomain(doc);
    }

    public Task CreateAsync(UserAccount userAccount, CancellationToken cancellationToken)
    {
        return _users.InsertOneAsync(MapToDocument(userAccount), cancellationToken: cancellationToken);
    }

    public async Task<bool> UpdateAsync(UserAccount userAccount, CancellationToken cancellationToken)
    {
        var result = await _users.ReplaceOneAsync(
            x => x.Id == userAccount.Id,
            MapToDocument(userAccount),
            cancellationToken: cancellationToken);

        return result.ModifiedCount == 1;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _users.DeleteOneAsync(x => x.Id == id, cancellationToken);
        return result.DeletedCount == 1;
    }

    public Task<long> CountInRoleAsync(string role, CancellationToken cancellationToken)
    {
        var normalizedRole = AuthRoles.Normalize(role);
        return _users.CountDocumentsAsync(x => x.Roles.Contains(normalizedRole), cancellationToken: cancellationToken);
    }

    public async Task<bool> AnyAsync(CancellationToken cancellationToken)
    {
        var count = await _users.CountDocumentsAsync(Builders<UserAccountDocument>.Filter.Empty, cancellationToken: cancellationToken);
        return count > 0;
    }

    private static UserAccountDocument MapToDocument(UserAccount user)
    {
        return new UserAccountDocument
        {
            Id = user.Id,
            Username = user.Username,
            NormalizedUsername = user.NormalizedUsername,
            PasswordHash = user.PasswordHash,
            Roles = user.Roles.ToArray(),
            CreatedAt = user.CreatedAt,
            CreatedBy = user.CreatedBy,
            RequiresPasswordChange = user.RequiresPasswordChange,
            LastPasswordChangedAt = user.LastPasswordChangedAt,
            IsLocked = user.IsLocked,
            LockedAt = user.LockedAt,
            FailedSignInAttempts = user.FailedSignInAttempts,
            TwoFactorSharedSecret = user.TwoFactorSharedSecret,
            TwoFactorEnabled = user.TwoFactorEnabled,
            RecoveryCodeHashes = user.RecoveryCodeHashes.ToArray()
        };
    }

    private static UserAccount MapToDomain(UserAccountDocument doc)
    {
        return UserAccount.Rehydrate(
            doc.Id,
            doc.Username,
            doc.PasswordHash,
            doc.Roles,
            doc.CreatedAt,
            doc.CreatedBy,
            doc.RequiresPasswordChange,
            doc.LastPasswordChangedAt,
            doc.IsLocked,
            doc.LockedAt,
            doc.FailedSignInAttempts,
            doc.TwoFactorSharedSecret,
            doc.TwoFactorEnabled,
            doc.RecoveryCodeHashes);
    }

    private sealed class UserAccountDocument
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string NormalizedUsername { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string[] Roles { get; set; } = [];

        public DateTime CreatedAt { get; set; }

        public string? CreatedBy { get; set; }

        public bool RequiresPasswordChange { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? LastPasswordChangedAt { get; set; }

        public bool IsLocked { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? LockedAt { get; set; }

        public int FailedSignInAttempts { get; set; }

        public string? TwoFactorSharedSecret { get; set; }

        public bool TwoFactorEnabled { get; set; }

        public string[] RecoveryCodeHashes { get; set; } = [];
    }
}