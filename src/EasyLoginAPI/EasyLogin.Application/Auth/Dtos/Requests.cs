namespace EasyLogin.Application.Auth.Dtos;

public record LoginRequest(string Email, string Password);

public record RegisterRequest(string FirstName, string LastName, string Email, string Password, string ConfirmPassword);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Email, string Token, string Password, string ConfirmPassword);

public record RefreshTokenRequest(string AccessToken, string RefreshToken);

public record RevokeTokenRequest(string RefreshToken);
