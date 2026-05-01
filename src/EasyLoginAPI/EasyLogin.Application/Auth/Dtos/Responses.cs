namespace EasyLogin.Application.Auth.Dtos;

public record AuthResponse(string AccessToken, string RefreshToken, int ExpiresIn);

public record UserProfileResponse(string Id, string FirstName, string LastName, string Email, IList<string> Roles);

public record UserListItemResponse(string Id, string FirstName, string LastName, string Email, bool IsActive, DateTimeOffset CreatedAt, IList<string> Roles);

public record UserDetailResponse(string Id, string FirstName, string LastName, string Email, bool IsActive, DateTimeOffset CreatedAt, IList<string> Roles);

public record RoleResponse(string Id, string Name, string? Description, bool IsSystemRole, DateTimeOffset CreatedAt);
