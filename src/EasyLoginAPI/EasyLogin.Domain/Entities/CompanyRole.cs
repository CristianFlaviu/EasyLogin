namespace EasyLogin.Domain.Entities;

public class CompanyRole : BaseEntity<Guid>
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required Guid CompanyId { get; set; }
}
