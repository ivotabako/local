using LocalEnterprise.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace LocalEnterprise.Infrastructure.Security;

public sealed class AspNetPasswordHashService : IPasswordHashService
{
    private readonly PasswordHasher<PasswordSubject> _hasher = new();

    public string HashPassword(string username, string password)
    {
        return _hasher.HashPassword(new PasswordSubject { Username = username }, password);
    }

    public bool VerifyHashedPassword(string username, string hashedPassword, string providedPassword)
    {
        var result = _hasher.VerifyHashedPassword(new PasswordSubject { Username = username }, hashedPassword, providedPassword);
        return result != PasswordVerificationResult.Failed;
    }

    private sealed class PasswordSubject
    {
        public string Username { get; init; } = string.Empty;
    }
}