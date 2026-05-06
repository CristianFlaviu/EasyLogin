using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record AcceptInviteCommand(string Token, string Password, string ConfirmPassword) : IRequest;

public class AcceptInviteCommandHandler(
    IUserRepository userRepository,
    IEmailService emailService,
    IEmailTemplateRenderer templateRenderer,
    IAuditLogger auditLogger)
    : IRequestHandler<AcceptInviteCommand>
{
    public async Task Handle(AcceptInviteCommand request, CancellationToken cancellationToken)
    {
        string tokenHash = HashHelper.Sha256(request.Token);
        (string UserId, string Email) accepted;

        try
        {
            accepted = await userRepository.AcceptInviteAsync(tokenHash, request.Password);
        }
        catch (Exception ex) when (ex is InviteTokenExpiredException or InviteTokenUsedException or KeyNotFoundException or InvalidOperationException)
        {
            await auditLogger.WriteAsync(new AuditEntry
            {
                EventType = AuditEventType.UserInviteAcceptFailed,
                Success = false,
                FailureReason = ex.Message
            }, cancellationToken);
            throw;
        }

        string body = await templateRenderer.RenderAsync("PasswordChanged", new Dictionary<string, string>
        {
            ["email"] = accepted.Email
        });
        await emailService.SendAsync(accepted.Email, "Your password was changed", body);

        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.UserInviteAccepted,
            Success = true,
            TargetType = AuditTargetType.User,
            TargetId = accepted.UserId,
            TargetDisplay = accepted.Email
        }, cancellationToken);
    }
}
