using Microsoft.AspNetCore.Identity;

namespace EasyLogin.Infrastructure.Identity;

public class AppIdentityRole : IdentityRole
{
    public required bool IsSystemRole { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? Description { get; set; }
}
