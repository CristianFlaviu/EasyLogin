using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record AdminCreateUserCommand(string FirstName, string LastName, string Email, string Password, IList<string> Roles)
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

        var user = await userRepository.CreateUserAsync(request.FirstName, request.LastName, request.Email, request.Password);

        var roles = request.Roles.Count > 0 ? request.Roles : new List<string> { "User" };
        foreach (var role in roles)
            await userRepository.AssignRoleAsync(user.Id, role);

        var body = await templateRenderer.RenderAsync("Welcome", new Dictionary<string, string>
        {
            ["firstName"] = request.FirstName,
            ["email"] = request.Email
        });
        await emailService.SendAsync(request.Email, "Welcome to EasyLogin", body);

        return new UserDetailResponse(user.Id, user.FirstName, user.LastName, user.Email, user.IsActive, user.CreatedAt, roles);
    }
}
