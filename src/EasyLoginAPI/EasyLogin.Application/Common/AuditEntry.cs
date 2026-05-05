namespace EasyLogin.Application.Common;

public class AuditEntry
{
    public required string EventType { get; init; }
    public required bool Success { get; init; }

    public string? ActorUserId { get; init; }
    public string? ActorEmail { get; init; }

    public string? TargetType { get; init; }
    public string? TargetId { get; init; }
    public string? TargetDisplay { get; init; }

    public string? FailureReason { get; init; }
    public string? Jti { get; init; }
    public IDictionary<string, string>? Metadata { get; init; }
}
