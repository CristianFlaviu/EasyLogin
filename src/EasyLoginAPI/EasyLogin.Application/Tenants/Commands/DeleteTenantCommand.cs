using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Tenants.Commands;

public record DeleteTenantCommand(Guid Id) : IRequest;

public class DeleteTenantCommandHandler(ITenantRepository tenantRepository)
    : IRequestHandler<DeleteTenantCommand>
{
    public Task Handle(DeleteTenantCommand request, CancellationToken cancellationToken)
        => tenantRepository.DeleteAsync(request.Id);
}
