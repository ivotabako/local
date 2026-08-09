namespace LocalEnterprise.Application.Identity;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);