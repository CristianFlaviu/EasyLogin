using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record DeleteRoleCommand(string RoleId) : IRequest;

public class DeleteRoleCommandHandler(IRoleRepository roleRepository)
    : IRequestHandler<DeleteRoleCommand>
{
    public Task Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        => roleRepository.DeleteRoleAsync(request.RoleId);
}
