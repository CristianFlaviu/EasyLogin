using Microsoft.AspNetCore.Identity;

namespace EasyLogin.Infrastructure.Identity;

public class AppIdentityRole : IdentityRole
{
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsSystemRole { get; set; }
}
