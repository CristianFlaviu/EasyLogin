namespace EasyLogin.Domain.Entities;

public class ApplicationRole
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsSystemRole { get; set; }
}
