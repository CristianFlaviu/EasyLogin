using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record EnableTwoFactorCommand(string UserId, string Password) : IRequest<TwoFactorSetupResponse>;

public class EnableTwoFactorCommandHandler(
    IUserRepository userRepository,
    ITwoFactorService twoFactorService,
    IAuditLogger auditLogger)
    : IRequestHandler<EnableTwoFactorCommand, TwoFactorSetupResponse>
{
    public async Task<TwoFactorSetupResponse> Handle(EnableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        ApplicationUser user = await userRepository.GetByIdAsync(request.UserId);
        if (user.TwoFactorEnabled)
            throw new InvalidOperationException("Two-factor authentication is already enabled.");

        if (await twoFactorService.IsLockedOutAsync(user.Id))
            throw new UnauthorizedAccessException();

        if (!await twoFactorService.CheckPasswordAsync(user.Id, request.Password))
        {
            await twoFactorService.AccessFailedAsync(user.Id);
            await auditLogger.WriteAsync(new AuditEntry
            {
                EventType = AuditEventType.TwoFactorVerificationFailed,
                Success = false,
                ActorUserId = user.Id,
                ActorEmail = user.Email,
                FailureReason = "InvalidPassword"
            }, cancellationToken);
            throw new UnauthorizedAccessException();
        }

        return await twoFactorService.BeginSetupAsync(user.Id, "EasyLogin");
    }
}
