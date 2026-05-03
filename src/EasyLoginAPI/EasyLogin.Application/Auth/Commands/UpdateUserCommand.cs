using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record UpdateUserCommand(
    string UserId, string FirstName, string LastName, string Email,
    bool IsActive, IList<string> SystemRoles, string? NewPassword,
    Guid? CallerCompanyId = null)
    : IRequest<UserDetailResponse>;

public class UpdateUserCommandHandler(IUserRepository userRepository)
    : IRequestHandler<UpdateUserCommand, UserDetailResponse>
{
    public async Task<UserDetailResponse> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        await userRepository.UpdateUserAsync(
            request.UserId, request.FirstName, request.LastName,
            request.Email, request.IsActive, request.SystemRoles,
            request.NewPassword, request.CallerCompanyId);

        var (user, systemRoles, companyRoles) = await userRepository.GetByIdWithRolesAsync(request.UserId, request.CallerCompanyId);
        return new UserDetailResponse(
            user.Id, user.FirstName, user.LastName, user.Email,
            user.IsActive, user.CreatedAt,
            user.CompanyId, user.CompanyName,
            systemRoles, companyRoles);
    }
}
