using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Companies.Commands;

public record CreateCompanyUserCommand(
    string FirstName, string LastName, string Email, string Password,
    IList<Guid> CompanyRoleIds, Guid CallerCompanyId)
    : IRequest<UserDetailResponse>;

public class CreateCompanyUserCommandHandler(
    IUserRepository userRepository,
    ICompanyRoleRepository companyRoleRepository,
    IEmailService emailService,
    IEmailTemplateRenderer templateRenderer)
    : IRequestHandler<CreateCompanyUserCommand, UserDetailResponse>
{
    public async Task<UserDetailResponse> Handle(CreateCompanyUserCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.EmailExistsAsync(request.Email))
            throw new InvalidOperationException($"Email '{request.Email}' is already registered.");

        var user = await userRepository.CreateUserAsync(
            request.FirstName, request.LastName, request.Email, request.Password, request.CallerCompanyId);

        if (request.CompanyRoleIds.Count > 0)
            await companyRoleRepository.UpdateUserRolesAsync(user.Id, request.CompanyRoleIds, request.CallerCompanyId);

        var body = await templateRenderer.RenderAsync("Welcome", new Dictionary<string, string>
        {
            ["firstName"] = request.FirstName,
            ["email"] = request.Email
        });
        await emailService.SendAsync(request.Email, "Welcome to EasyLogin", body);

        var (detail, systemRoles, companyRoles) = await userRepository.GetByIdWithRolesAsync(user.Id, request.CallerCompanyId);
        return new UserDetailResponse(
            detail.Id, detail.FirstName, detail.LastName, detail.Email,
            detail.IsActive, detail.CreatedAt,
            detail.CompanyId, detail.CompanyName,
            systemRoles, companyRoles);
    }
}
