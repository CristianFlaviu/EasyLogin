using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record UpdateUserCommand(string UserId, string FirstName, string LastName, string Email, bool IsActive, IList<string> Roles, string? NewPassword)
    : IRequest<UserDetailResponse>;

public class UpdateUserCommandHandler(IUserRepository userRepository)
    : IRequestHandler<UpdateUserCommand, UserDetailResponse>
{
    public async Task<UserDetailResponse> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        await userRepository.UpdateUserAsync(
            request.UserId, request.FirstName, request.LastName,
            request.Email, request.IsActive, request.Roles, request.NewPassword);

        var (user, roles) = await userRepository.GetByIdWithRolesAsync(request.UserId);
        return new UserDetailResponse(user.Id, user.FirstName, user.LastName, user.Email, user.IsActive, user.CreatedAt, roles);
    }
}
