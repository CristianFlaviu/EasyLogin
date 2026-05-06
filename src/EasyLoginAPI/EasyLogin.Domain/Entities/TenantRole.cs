namespace EasyLogin.Domain.Entities;

public class TenantRole : BaseEntity<Guid>
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required Guid TenantId { get; set; }
}
