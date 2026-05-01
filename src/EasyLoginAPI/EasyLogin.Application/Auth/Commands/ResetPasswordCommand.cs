using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record ResetPasswordCommand(string Email, string Token, string Password) : IRequest;

public class ResetPasswordCommandHandler(
    IUserRepository userRepository,
    IEmailService emailService,
    IEmailTemplateRenderer templateRenderer)
    : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        await userRepository.ResetPasswordAsync(request.Email, request.Token, request.Password);

        var body = await templateRenderer.RenderAsync("PasswordChanged", new Dictionary<string, string>
        {
            ["email"] = request.Email
        });
        await emailService.SendAsync(request.Email, "Your password was changed", body);
    }
}
