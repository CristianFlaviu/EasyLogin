namespace EasyLogin.Domain.Entities;

public class ApplicationUser : BaseEntity<string>
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required UserStatus Status { get; set; }
    public bool IsActive => Status == UserStatus.Active;
    public string? RefreshTokenHash { get; set; }
    public DateTimeOffset? RefreshTokenExpiry { get; set; }
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
}
