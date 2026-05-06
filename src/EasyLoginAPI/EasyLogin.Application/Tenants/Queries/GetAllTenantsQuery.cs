using EasyLogin.Application.Tenants.Dtos;
using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Tenants.Queries;

public record GetAllTenantsQuery : IRequest<IList<TenantResponse>>;

public class GetAllTenantsQueryHandler(ITenantRepository tenantRepository)
    : IRequestHandler<GetAllTenantsQuery, IList<TenantResponse>>
{
    public Task<IList<TenantResponse>> Handle(GetAllTenantsQuery request, CancellationToken cancellationToken)
        => tenantRepository.GetAllAsync();
}
