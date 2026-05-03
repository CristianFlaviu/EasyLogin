namespace EasyLogin.Domain.Entities;

public class ApplicationUser
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? RefreshTokenHash { get; set; }
    public DateTimeOffset? RefreshTokenExpiry { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }

    public ApplicationUser()
    {
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
