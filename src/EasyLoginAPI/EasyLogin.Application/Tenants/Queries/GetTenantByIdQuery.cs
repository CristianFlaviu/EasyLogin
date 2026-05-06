using EasyLogin.Application.Tenants.Dtos;
using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Tenants.Queries;

public record GetTenantByIdQuery(Guid Id) : IRequest<TenantResponse>;

public class GetTenantByIdQueryHandler(ITenantRepository tenantRepository)
    : IRequestHandler<GetTenantByIdQuery, TenantResponse>
{
    public Task<TenantResponse> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
        => tenantRepository.GetByIdAsync(request.Id);
}
