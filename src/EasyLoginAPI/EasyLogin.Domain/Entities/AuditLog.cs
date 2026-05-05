namespace EasyLogin.Domain.Entities;

public class AuditLog
{
    public required Guid Id { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
    public required string EventType { get; set; }
    public required bool Success { get; set; }

    public string? ActorUserId { get; set; }
    public string? ActorEmail { get; set; }

    public string? TargetType { get; set; }
    public string? TargetId { get; set; }
    public string? TargetDisplay { get; set; }

    public string? FailureReason { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? BrowserName { get; set; }
    public string? BrowserVersion { get; set; }
    public string? OsName { get; set; }
    public string? OsVersion { get; set; }
    public string? DeviceFamily { get; set; }

    public string? Jti { get; set; }
    public string? CorrelationId { get; set; }
    public string? MetadataJson { get; set; }
}
