using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record SendEmailTwoFactorCodeCommand(string UserId) : IRequest;

public class SendEmailTwoFactorCodeCommandHandler(
    IUserRepository userRepository,
    ITwoFactorService twoFactorService,
    IEmailService emailService,
    IEmailTemplateRenderer templateRenderer)
    : IRequestHandler<SendEmailTwoFactorCodeCommand>
{
    public async Task Handle(SendEmailTwoFactorCodeCommand request, CancellationToken cancellationToken)
    {
        ApplicationUser user = await userRepository.GetByIdAsync(request.UserId);
        if (!user.EmailConfirmed)
            throw new InvalidOperationException("Email address is not confirmed.");

        if (await twoFactorService.IsLockedOutAsync(user.Id))
            throw new UnauthorizedAccessException();

        string code = await twoFactorService.GenerateEmailTwoFactorCodeAsync(user.Id);
        string body = await templateRenderer.RenderAsync("TwoFactorCode", new Dictionary<string, string>
        {
            ["firstName"] = user.FirstName,
            ["code"] = code,
            ["minutes"] = "5"
        });

        await emailService.SendAsync(user.Email, "Your EasyLogin verification code", body);
    }
}
