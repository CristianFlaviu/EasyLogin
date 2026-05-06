using EasyLogin.Application.Tenants.Dtos;
using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Tenants.Queries;

public record GetTenantRolesQuery(Guid TenantId) : IRequest<IList<TenantRoleResponse>>;

public class GetTenantRolesQueryHandler(ITenantRoleRepository tenantRoleRepository)
    : IRequestHandler<GetTenantRolesQuery, IList<TenantRoleResponse>>
{
    public Task<IList<TenantRoleResponse>> Handle(GetTenantRolesQuery request, CancellationToken cancellationToken)
        => tenantRoleRepository.GetByTenantIdAsync(request.TenantId);
}
