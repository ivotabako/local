namespace LocalEnterprise.Auth.Security;

public sealed class AuthUser
{
    public required string Username { get; init; }
    public required string Password { get; init; }
    public string[] Roles { get; init; } = [];
}
