using EasyLogin.Application.Tenants.Dtos;
using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Tenants.Commands;

public record UpdateTenantCommand(Guid Id, string Name, bool IsActive) : IRequest<TenantResponse>;

public class UpdateTenantCommandHandler(ITenantRepository tenantRepository)
    : IRequestHandler<UpdateTenantCommand, TenantResponse>
{
    public Task<TenantResponse> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
        => tenantRepository.UpdateAsync(request.Id, request.Name, request.IsActive);
}
