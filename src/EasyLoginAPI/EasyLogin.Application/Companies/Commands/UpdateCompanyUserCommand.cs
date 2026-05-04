using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Companies.Commands;

public record UpdateCompanyUserCommand(
    string UserId, string FirstName, string LastName, string Email,
    bool IsActive, IList<Guid> CompanyRoleIds, string? NewPassword,
    Guid CallerCompanyId)
    : IRequest<UserDetailResponse>;

public class UpdateCompanyUserCommandHandler(
    IUserRepository userRepository,
    ICompanyRoleRepository companyRoleRepository)
    : IRequestHandler<UpdateCompanyUserCommand, UserDetailResponse>
{
    public async Task<UserDetailResponse> Handle(UpdateCompanyUserCommand request, CancellationToken cancellationToken)
    {
        await userRepository.UpdateUserAsync(
            request.UserId, request.FirstName, request.LastName,
            request.Email, request.IsActive, null,
            request.NewPassword, request.CallerCompanyId);

        await companyRoleRepository.UpdateUserRolesAsync(
            request.UserId, request.CompanyRoleIds, request.CallerCompanyId);

        var (user, systemRoles, companyRoles) = await userRepository.GetByIdWithRolesAsync(request.UserId, request.CallerCompanyId);
        return new UserDetailResponse(
            user.Id, user.FirstName, user.LastName, user.Email,
            user.IsActive, user.CreatedAt, user.UpdatedAt,
            user.CompanyId, user.CompanyName,
            systemRoles, companyRoles);
    }
}
