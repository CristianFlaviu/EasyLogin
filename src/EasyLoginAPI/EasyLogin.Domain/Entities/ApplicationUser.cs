namespace EasyLogin.Domain.Entities;

public class ApplicationUser : BaseEntity<string>
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required UserStatus Status { get; set; }
    public bool IsActive => Status == UserStatus.Active;
    public bool EmailConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public TwoFactorMethod? TwoFactorMethod { get; set; }
    public string? RefreshTokenHash { get; set; }
    public DateTimeOffset? RefreshTokenExpiry { get; set; }
    public Guid? TenantId { get; set; }
    public string? TenantName { get; set; }
}

public enum TwoFactorMethod
{
    Authenticator = 0,
    Email = 1
}
