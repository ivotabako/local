namespace LocalEnterprise.Application.Abstractions;

public interface IPasswordHashService
{
    string HashPassword(string username, string password);
    bool VerifyHashedPassword(string username, string hashedPassword, string providedPassword);
}