namespace EasyLogin.Application.Companies.Dtos;

public record CreateCompanyRequest(string Name);

public record UpdateCompanyRequest(string Name, bool IsActive);

public record CreateCompanyRoleRequest(string Name, string? Description);
