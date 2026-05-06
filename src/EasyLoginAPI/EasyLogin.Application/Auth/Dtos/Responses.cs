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
    IList<string> Roles, IList<string> CompanyRoles,
    string Status);

public record UserDetailResponse(
    string Id, string FirstName, string LastName, string Email,
    bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt,
    Guid? CompanyId, string? CompanyName,
    IList<string> Roles, IList<string> CompanyRoles,
    string Status);

public record InviteValidationResponse(string Email, string FirstName, string LastName);

public record RoleResponse(string Id, string Name, string? Description, bool IsSystemRole, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);

public record AuditLogResponse(
    Guid Id,
    DateTimeOffset Timestamp,
    string EventType,
    bool Success,
    string? ActorUserId,
    string? ActorEmail,
    string? TargetType,
    string? TargetId,
    string? TargetDisplay,
    string? FailureReason,
    string? IpAddress,
    string? UserAgent,
    string? BrowserName,
    string? BrowserVersion,
    string? OsName,
    string? OsVersion,
    string? DeviceFamily,
    string? Jti,
    string? CorrelationId,
    string? MetadataJson);
