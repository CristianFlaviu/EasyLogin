using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record ResetPasswordCommand(string Email, string Token, string Password) : IRequest;

public class ResetPasswordCommandHandler(
    IUserRepository userRepository,
    IEmailService emailService,
    IEmailTemplateRenderer templateRenderer,
    IAuditLogger auditLogger)
    : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await userRepository.ResetPasswordAsync(request.Email, request.Token, request.Password);
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
        {
            await auditLogger.WriteAsync(new AuditEntry
            {
                EventType = AuditEventType.ResetPasswordFailed,
                Success = false,
                ActorEmail = request.Email,
                FailureReason = ex.Message
            }, cancellationToken);
            throw;
        }

        var body = await templateRenderer.RenderAsync("PasswordChanged", new Dictionary<string, string>
        {
            ["email"] = request.Email
        });
        await emailService.SendAsync(request.Email, "Your password was changed", body);

        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.ResetPassword,
            Success = true,
            ActorEmail = request.Email
        }, cancellationToken);
    }
}
