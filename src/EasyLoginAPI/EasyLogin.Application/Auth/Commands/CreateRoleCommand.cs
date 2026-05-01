using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record CreateRoleCommand(string Name, string? Description) : IRequest<RoleResponse>;

public class CreateRoleCommandHandler(IRoleRepository roleRepository)
    : IRequestHandler<CreateRoleCommand, RoleResponse>
{
    public Task<RoleResponse> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        => roleRepository.CreateRoleAsync(request.Name, request.Description);
}
