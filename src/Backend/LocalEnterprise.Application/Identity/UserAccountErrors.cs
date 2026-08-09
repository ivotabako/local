namespace LocalEnterprise.Application.Identity;

public static class UserAccountErrors
{
    public const string UserNotFound = "UserNotFound";
    public const string DuplicateUsername = "DuplicateUsername";
    public const string InvalidUsername = "InvalidUsername";
    public const string InvalidPassword = "InvalidPassword";
    public const string InvalidRoles = "InvalidRoles";
    public const string InvalidCredentials = "InvalidCredentials";
    public const string WeakPassword = "WeakPassword";
    public const string IncorrectCurrentPassword = "IncorrectCurrentPassword";
    public const string LockedOut = "LockedOut";
    public const string TwoFactorNotConfigured = "TwoFactorNotConfigured";
    public const string InvalidTwoFactorCode = "InvalidTwoFactorCode";
    public const string CannotDeleteLastAdmin = "CannotDeleteLastAdmin";
    public const string CannotDeleteCurrentUser = "CannotDeleteCurrentUser";
    public const string CannotRemoveLastAdminRole = "CannotRemoveLastAdminRole";
}