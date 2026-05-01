using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Auth.Queries;

public record GetUserByIdQuery(string UserId) : IRequest<UserDetailResponse>;

public class GetUserByIdQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetUserByIdQuery, UserDetailResponse>
{
    public async Task<UserDetailResponse> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var (user, roles) = await userRepository.GetByIdWithRolesAsync(request.UserId);
        return new UserDetailResponse(user.Id, user.FirstName, user.LastName, user.Email, user.IsActive, user.CreatedAt, roles);
    }
}
