using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record ForgotPasswordCommand(string Email) : IRequest;

public class ForgotPasswordCommandHandler(IUserRepository userRepository, IEmailService emailService)
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

        var body = $"""
            <p>You requested a password reset.</p>
            <p>Use this token to reset your password:</p>
            <pre>{encodedToken}</pre>
            <p>This token expires in 24 hours.</p>
            """;

        await emailService.SendAsync(request.Email, "Reset your password", body);
    }
}
