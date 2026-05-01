using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record ForgotPasswordCommand(string Email) : IRequest;

public class ForgotPasswordCommandHandler(
    IUserRepository userRepository,
    IEmailService emailService,
    IEmailTemplateRenderer templateRenderer)
    : IRequestHandler<ForgotPasswordCommand>
{
    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        string token;
        try
        {
            token = await userRepository.GeneratePasswordResetTokenAsync(request.Email);
        }
        catch (KeyNotFoundException)
        {
            return;
        }

        var encodedToken = Uri.EscapeDataString(token);
        var body = await templateRenderer.RenderAsync("ForgotPassword", new Dictionary<string, string>
        {
            ["token"] = encodedToken
        });

        await emailService.SendAsync(request.Email, "Reset your password", body);
    }
}
