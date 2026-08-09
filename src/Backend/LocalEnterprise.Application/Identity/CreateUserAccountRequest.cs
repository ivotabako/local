namespace LocalEnterprise.Application.Identity;

public sealed record CreateUserAccountRequest(string Username, string Password, string[] Roles);