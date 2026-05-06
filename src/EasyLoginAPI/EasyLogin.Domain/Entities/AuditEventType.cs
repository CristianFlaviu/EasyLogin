namespace EasyLogin.Domain.Entities;

public static class AuditEventType
{
    public const string LoginSuccess = "LoginSuccess";
    public const string LoginFailed = "LoginFailed";
    public const string Register = "Register";
    public const string RefreshToken = "RefreshToken";
    public const string RefreshTokenFailed = "RefreshTokenFailed";
    public const string RevokeToken = "RevokeToken";
    public const string ForgotPassword = "ForgotPassword";
    public const string ResetPassword = "ResetPassword";
    public const string ResetPasswordFailed = "ResetPasswordFailed";

    public const string UserCreated = "UserCreated";
    public const string UserCreateFailed = "UserCreateFailed";
    public const string UserUpdated = "UserUpdated";
    public const string UserUpdateFailed = "UserUpdateFailed";
    public const string UserDeleted = "UserDeleted";
    public const string UserDeleteFailed = "UserDeleteFailed";
    public const string UserInvited = "UserInvited";
    public const string UserInviteResent = "UserInviteResent";
    public const string UserInviteAccepted = "UserInviteAccepted";
    public const string UserInviteAcceptFailed = "UserInviteAcceptFailed";
    public const string UserInviteValidateFailed = "UserInviteValidateFailed";

    public const string RoleCreated = "RoleCreated";
    public const string RoleCreateFailed = "RoleCreateFailed";
    public const string RoleDeleted = "RoleDeleted";
    public const string RoleDeleteFailed = "RoleDeleteFailed";
}

public static class AuditTargetType
{
    public const string User = "User";
    public const string Role = "Role";
    public const string CompanyRole = "CompanyRole";
}
