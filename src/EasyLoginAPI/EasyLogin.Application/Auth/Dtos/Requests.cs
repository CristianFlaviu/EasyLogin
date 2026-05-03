namespace EasyLogin.Application.Auth.Dtos;

public record LoginRequest(string Email, string Password);

public record RegisterRequest(string FirstName, string LastName, string Email, string Password, string ConfirmPassword);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Email, string Token, string Password, string ConfirmPassword);

public record RefreshTokenRequest(string AccessToken, string RefreshToken);

public record RevokeTokenRequest(string RefreshToken);

public record AdminCreateUserRequest(string FirstName, string LastName, string Email, string Password, List<string> SystemRoles, Guid? CompanyId = null);

public record CreateCompanyUserRequest(string FirstName, string LastName, string Email, string Password, List<Guid> CompanyRoleIds);

public record UpdateUserRequest(string FirstName, string LastName, string Email, bool IsActive, List<string> SystemRoles, string? NewPassword);

public record UpdateCompanyUserRequest(string FirstName, string LastName, string Email, bool IsActive, List<Guid> CompanyRoleIds, string? NewPassword);

public record CreateRoleRequest(string Name, string? Description);
