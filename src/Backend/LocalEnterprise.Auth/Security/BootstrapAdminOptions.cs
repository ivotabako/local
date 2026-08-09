using LocalEnterprise.Domain.Identity;

namespace LocalEnterprise.Auth.Security;

public sealed class BootstrapAdminOptions
{
    public const string SectionName = "Auth:BootstrapAdmin";

    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string[] Roles { get; init; } = [AuthRoles.Admin];
}