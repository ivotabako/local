namespace LocalEnterprise.Domain.Identity;

public static class AuthRoles
{
    public const string Admin = "Admin";
    public const string Reader = "Reader";
    public const string Writer = "Writer";

    public static readonly string[] All = [Admin, Reader, Writer];

    public static bool IsAllowed(string? role)
    {
        return role is not null && All.Any(x => string.Equals(x, role.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static string Normalize(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("Role is required.", nameof(role));
        }

        var match = All.FirstOrDefault(x => string.Equals(x, role.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            throw new ArgumentOutOfRangeException(nameof(role), $"Role '{role}' is not supported.");
        }

        return match;
    }
}