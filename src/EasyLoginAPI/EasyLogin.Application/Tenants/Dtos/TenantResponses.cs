namespace EasyLogin.Application.Tenants.Dtos;

public record TenantResponse(Guid Id, string Name, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);

public record TenantRoleResponse(Guid Id, string Name, string? Description, Guid TenantId, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);
