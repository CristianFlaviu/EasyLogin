using EasyLogin.Application.Tenants.Dtos;
using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Tenants.Commands;

public record CreateTenantCommand(string Name) : IRequest<TenantResponse>;

public class CreateTenantCommandHandler(ITenantRepository tenantRepository)
    : IRequestHandler<CreateTenantCommand, TenantResponse>
{
    public Task<TenantResponse> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
        => tenantRepository.CreateAsync(request.Name);
}
