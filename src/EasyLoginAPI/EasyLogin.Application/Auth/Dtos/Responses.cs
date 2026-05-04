namespace EasyLogin.Application.Auth.Dtos;

public record AuthResponse(string AccessToken, string RefreshToken, int ExpiresIn);

public record UserProfileResponse(
    string Id, string FirstName, string LastName, string Email,
    Guid? CompanyId, string? CompanyName,
    IList<string> Roles, IList<string> CompanyRoles);

public record UserListItemResponse(
    string Id, string FirstName, string LastName, string Email,
    bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt,
    Guid? CompanyId, string? CompanyName,
    IList<string> Roles, IList<string> CompanyRoles);

public record UserDetailResponse(
    string Id, string FirstName, string LastName, string Email,
    bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt,
    Guid? CompanyId, string? CompanyName,
    IList<string> Roles, IList<string> CompanyRoles);

public record RoleResponse(string Id, string Name, string? Description, bool IsSystemRole, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);
