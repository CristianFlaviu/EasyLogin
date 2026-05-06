namespace EasyLogin.Domain.Entities;

public class InviteToken
{
    public required Guid Id { get; set; }
    public required string UserId { get; set; }
    public required string TokenHash { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public required bool IsUsed { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
}
