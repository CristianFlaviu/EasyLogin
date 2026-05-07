namespace EasyLogin.Application.Auth.Dtos;

public record AuthResponse(
    string? AccessToken,
    string? RefreshToken,
    int ExpiresIn,
    bool RequiresTwoFactor = false,
    string? TwoFactorToken = null,
    string? TwoFactorMethod = null);

public record TwoFactorSetupResponse(string OtpAuthUri, string SharedSecret);

public record OverviewResponse(int TotalUsers, int LoginsLast24Hours, int ActiveSessions);

public record OverviewLoginResponse(
    Guid Id,
    DateTimeOffset Timestamp,
    string? ActorUserId,
    string? ActorEmail,
    string? IpAddress,
    string? BrowserName,
    string? OsName,
    string? DeviceFamily);

public record OverviewActiveSessionResponse(
    string UserId,
    string FirstName,
    string LastName,
    string Email,
    Guid? TenantId,
    string? TenantName,
    DateTimeOffset? RefreshTokenExpiry);

public record UserProfileResponse(
    string Id, string FirstName, string LastName, string Email,
    Guid? TenantId, string? TenantName,
    IList<string> Roles, IList<string> TenantRoles,
    bool TwoFactorEnabled,
    string? TwoFactorMethod,
    bool EmailConfirmed);

public record UserListItemResponse(
    string Id, string FirstName, string LastName, string Email,
    bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt,
    Guid? TenantId, string? TenantName,
    IList<string> Roles, IList<string> TenantRoles,
    string Status);

public record UserDetailResponse(
    string Id, string FirstName, string LastName, string Email,
    bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt,
    Guid? TenantId, string? TenantName,
    IList<string> Roles, IList<string> TenantRoles,
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
