using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Auth.Queries;

public record GetCurrentUserQuery : IRequest<UserProfileResponse>;

public class GetCurrentUserQueryHandler(IUserRepository userRepository, ICurrentUserService currentUserService)
    : IRequestHandler<GetCurrentUserQuery, UserProfileResponse>
{
    public async Task<UserProfileResponse> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedAccessException();

        var (user, roles) = await userRepository.GetByIdWithRolesAsync(userId);

        return new UserProfileResponse(user.Id, user.FirstName, user.LastName, user.Email, roles);
    }
}
