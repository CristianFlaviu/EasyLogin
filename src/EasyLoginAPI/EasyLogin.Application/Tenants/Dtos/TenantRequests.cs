namespace EasyLogin.Application.Tenants.Dtos;

public record CreateTenantRequest(string Name);

public record UpdateTenantRequest(string Name, bool IsActive);

public record CreateTenantRoleRequest(string Name, string? Description);
