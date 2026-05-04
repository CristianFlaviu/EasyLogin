namespace EasyLogin.Domain.Entities;

public class UserCompanyRole
{
    public required string UserId { get; set; }
    public required Guid CompanyRoleId { get; set; }
}
