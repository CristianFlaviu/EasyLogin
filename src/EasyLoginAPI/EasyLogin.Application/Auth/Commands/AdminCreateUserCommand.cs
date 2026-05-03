using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record AdminCreateUserCommand(
    string FirstName, string LastName, string Email, string Password,
    IList<string> SystemRoles, Guid? CompanyId)
    : IRequest<UserDetailResponse>;

public class AdminCreateUserCommandHandler(
    IUserRepository userRepository,
    IEmailService emailService,
    IEmailTemplateRenderer templateRenderer)
    : IRequestHandler<AdminCreateUserCommand, UserDetailResponse>
{
    public async Task<UserDetailResponse> Handle(AdminCreateUserCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.EmailExistsAsync(request.Email))
            throw new InvalidOperationException($"Email '{request.Email}' is already registered.");

        var user = await userRepository.CreateUserAsync(
            request.FirstName, request.LastName, request.Email, request.Password, request.CompanyId);

        foreach (var role in request.SystemRoles)
            await userRepository.AssignRoleAsync(user.Id, role);

        var body = await templateRenderer.RenderAsync("Welcome", new Dictionary<string, string>
        {
            ["firstName"] = request.FirstName,
            ["email"] = request.Email
        });
        await emailService.SendAsync(request.Email, "Welcome to EasyLogin", body);

        var (detail, systemRoles, companyRoles) = await userRepository.GetByIdWithRolesAsync(user.Id);
        return new UserDetailResponse(
            detail.Id, detail.FirstName, detail.LastName, detail.Email,
            detail.IsActive, detail.CreatedAt,
            detail.CompanyId, detail.CompanyName,
            systemRoles, companyRoles);
    }
}
