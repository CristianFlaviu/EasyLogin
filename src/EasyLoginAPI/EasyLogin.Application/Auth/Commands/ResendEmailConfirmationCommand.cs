using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record ResendEmailConfirmationCommand(string Email) : IRequest;

public class ResendEmailConfirmationCommandHandler(
    IUserRepository userRepository,
    IEmailService emailService,
    IEmailTemplateRenderer templateRenderer,
    IAppUrlProvider appUrlProvider,
    IAuditLogger auditLogger)
    : IRequestHandler<ResendEmailConfirmationCommand>
{
    public async Task Handle(ResendEmailConfirmationCommand request, CancellationToken cancellationToken)
    {
        ApplicationUser? user = await userRepository.GetByEmailAsync(request.Email);
        if (user is null || user.EmailConfirmed)
            return;

        string token = await userRepository.GenerateEmailConfirmationTokenAsync(request.Email);
        string confirmationUrl = BuildConfirmationUrl(request.Email, token);
        string body = await templateRenderer.RenderAsync("ConfirmEmail", new Dictionary<string, string>
        {
            ["firstName"] = user.FirstName,
            ["confirmationUrl"] = confirmationUrl
        });

        await emailService.SendAsync(request.Email, "Confirm your EasyLogin email", body);

        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.EmailConfirmationSent,
            Success = true,
            ActorUserId = user.Id,
            ActorEmail = user.Email
        }, cancellationToken);
    }

    private string BuildConfirmationUrl(string email, string token)
    {
        string encodedEmail = Uri.EscapeDataString(email);
        string encodedToken = Uri.EscapeDataString(token);
        return $"{appUrlProvider.FrontendBaseUrl.TrimEnd('/')}/confirm-email?email={encodedEmail}&token={encodedToken}";
    }
}
