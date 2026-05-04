namespace EasyLogin.Application.Companies.Dtos;

public record CompanyResponse(Guid Id, string Name, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);

public record CompanyRoleResponse(Guid Id, string Name, string? Description, Guid CompanyId, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);
