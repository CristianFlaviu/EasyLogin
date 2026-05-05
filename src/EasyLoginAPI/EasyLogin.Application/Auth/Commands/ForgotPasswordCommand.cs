using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record ForgotPasswordCommand(string Email) : IRequest;

public class ForgotPasswordCommandHandler(
    IUserRepository userRepository,
    IEmailService emailService,
    IEmailTemplateRenderer templateRenderer,
    IAuditLogger auditLogger)
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
            await auditLogger.WriteAsync(new AuditEntry
            {
                EventType = AuditEventType.ForgotPassword,
                Success = false,
                ActorEmail = request.Email,
                FailureReason = "UserNotFound"
            }, cancellationToken);
            return;
        }

        var encodedToken = Uri.EscapeDataString(token);
        var body = await templateRenderer.RenderAsync("ForgotPassword", new Dictionary<string, string>
        {
            ["token"] = encodedToken
        });

        await emailService.SendAsync(request.Email, "Reset your password", body);

        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.ForgotPassword,
            Success = true,
            ActorEmail = request.Email
        }, cancellationToken);
    }
}
