using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Auth.Queries;

public record GetAllRolesQuery : IRequest<IList<RoleResponse>>;

public class GetAllRolesQueryHandler(IRoleRepository roleRepository)
    : IRequestHandler<GetAllRolesQuery, IList<RoleResponse>>
{
    public async Task<IList<RoleResponse>> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
        => await roleRepository.GetAllRolesAsync();
}
