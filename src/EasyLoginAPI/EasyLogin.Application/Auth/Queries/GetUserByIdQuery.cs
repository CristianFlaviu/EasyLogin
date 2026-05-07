using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Auth.Queries;

public record GetUserByIdQuery(string UserId, Guid? RequiredTenantId = null) : IRequest<UserDetailResponse>;

public class GetUserByIdQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetUserByIdQuery, UserDetailResponse>
{
    public async Task<UserDetailResponse> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var (user, systemRoles, tenantRoles) = await userRepository.GetByIdWithRolesAsync(request.UserId, request.RequiredTenantId);
        return new UserDetailResponse(
            user.Id, user.FirstName, user.LastName, user.Email,
            user.CreatedAt, user.UpdatedAt,
            user.TenantId, user.TenantName,
            systemRoles, tenantRoles, user.Status.ToDto());
    }
}
