using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Companies.Commands;

public record DeleteCompanyRoleCommand(Guid RoleId, Guid CallerCompanyId) : IRequest;

public class DeleteCompanyRoleCommandHandler(ICompanyRoleRepository companyRoleRepository)
    : IRequestHandler<DeleteCompanyRoleCommand>
{
    public Task Handle(DeleteCompanyRoleCommand request, CancellationToken cancellationToken)
        => companyRoleRepository.DeleteAsync(request.RoleId, request.CallerCompanyId);
}
