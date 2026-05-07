using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record ConfirmEmailCommand(string Email, string Token) : IRequest;

public class ConfirmEmailCommandHandler(
    IUserRepository userRepository,
    IEmailService emailService,
    IEmailTemplateRenderer templateRenderer,
    IAuditLogger auditLogger)
    : IRequestHandler<ConfirmEmailCommand>
{
    public async Task Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        await userRepository.ConfirmEmailAsync(request.Email, request.Token);

        ApplicationUser? user = await userRepository.GetByEmailAsync(request.Email);
        string firstName = user?.FirstName ?? request.Email;
        string body = await templateRenderer.RenderAsync("Welcome", new Dictionary<string, string>
        {
            ["firstName"] = firstName,
            ["email"] = request.Email
        });
        await emailService.SendAsync(request.Email, "Welcome to EasyLogin", body);

        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.EmailConfirmed,
            Success = true,
            ActorUserId = user?.Id,
            ActorEmail = request.Email
        }, cancellationToken);
    }
}
