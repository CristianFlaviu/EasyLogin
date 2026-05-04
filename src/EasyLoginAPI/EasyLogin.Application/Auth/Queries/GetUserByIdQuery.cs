using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Auth.Queries;

public record GetUserByIdQuery(string UserId, Guid? RequiredCompanyId = null) : IRequest<UserDetailResponse>;

public class GetUserByIdQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetUserByIdQuery, UserDetailResponse>
{
    public async Task<UserDetailResponse> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var (user, systemRoles, companyRoles) = await userRepository.GetByIdWithRolesAsync(request.UserId, request.RequiredCompanyId);
        return new UserDetailResponse(
            user.Id, user.FirstName, user.LastName, user.Email,
            user.IsActive, user.CreatedAt, user.UpdatedAt,
            user.CompanyId, user.CompanyName,
            systemRoles, companyRoles);
    }
}
