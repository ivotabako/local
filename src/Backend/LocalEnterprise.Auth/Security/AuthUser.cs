namespace LocalEnterprise.Auth.Security;

public sealed class AuthUser
{
    public required string Username { get; init; }
    public string PasswordHash { get; init; } = string.Empty;
    public string[] Roles { get; init; } = [];
}
