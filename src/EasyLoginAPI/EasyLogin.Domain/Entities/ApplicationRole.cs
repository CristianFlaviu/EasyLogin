namespace EasyLogin.Domain.Entities;

public class ApplicationRole : BaseEntity<string>
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required bool IsSystemRole { get; set; }
}
