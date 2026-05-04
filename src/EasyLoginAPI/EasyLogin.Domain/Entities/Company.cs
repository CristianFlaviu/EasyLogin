namespace EasyLogin.Domain.Entities;

public class Company : BaseEntity<Guid>
{
    public required string Name { get; set; }
    public required bool IsActive { get; set; }
}
