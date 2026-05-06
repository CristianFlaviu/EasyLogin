using Microsoft.AspNetCore.Identity;
using EasyLogin.Domain.Entities;

namespace EasyLogin.Infrastructure.Identity;

public class AppIdentityUser : IdentityUser
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public bool IsActive => Status == UserStatus.Active;
    public required DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? RefreshTokenHash { get; set; }
    public DateTimeOffset? RefreshTokenExpiry { get; set; }
    public Guid? CompanyId { get; set; }
}
