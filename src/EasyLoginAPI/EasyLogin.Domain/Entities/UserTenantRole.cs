namespace EasyLogin.Domain.Entities;

public class UserTenantRole
{
    public required string UserId { get; set; }
    public required Guid TenantRoleId { get; set; }
}
