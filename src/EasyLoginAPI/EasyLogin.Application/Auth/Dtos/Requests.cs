namespace EasyLogin.Application.Auth.Dtos;

public record LoginRequest(string Email, string Password);

public record RegisterRequest(string FirstName, string LastName, string Email, string Password, string ConfirmPassword);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Email, string Token, string Password, string ConfirmPassword);

public record RefreshTokenRequest(string AccessToken, string RefreshToken);

public record RevokeTokenRequest(string RefreshToken);

public record AdminCreateUserRequest(string FirstName, string LastName, string Email, string Password, List<string> SystemRoles, Guid? TenantId = null);

public record InviteUserRequest(string FirstName, string LastName, string Email, List<string> SystemRoles, Guid? TenantId);

public record CreateTenantUserRequest(string FirstName, string LastName, string Email, string Password, List<Guid> TenantRoleIds);

public record UpdateUserRequest(string FirstName, string LastName, string Email, bool IsActive, List<string> SystemRoles, string? NewPassword);

public record UpdateTenantUserRequest(string FirstName, string LastName, string Email, bool IsActive, List<Guid> TenantRoleIds, string? NewPassword);

public record AcceptInviteRequest(string Token, string Password, string ConfirmPassword);

public record CreateRoleRequest(string Name, string? Description);
