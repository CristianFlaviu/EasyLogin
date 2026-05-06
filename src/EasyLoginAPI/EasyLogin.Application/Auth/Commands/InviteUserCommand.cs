using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record InviteUserCommand(
    string FirstName, string LastName, string Email,
    IList<string> SystemRoles, Guid? CompanyId)
    : IRequest<UserDetailResponse>;

public class InviteUserCommandHandler(
    IUserRepository userRepository,
    IEmailService emailService,
    IEmailTemplateRenderer templateRenderer,
    IAuditLogger auditLogger,
    IAppUrlProvider appUrlProvider)
    : IRequestHandler<InviteUserCommand, UserDetailResponse>
{
    private const int ExpiryHours = 48;

    public async Task<UserDetailResponse> Handle(InviteUserCommand request, CancellationToken cancellationToken)
    {
        ApplicationUser? existing = await userRepository.GetByEmailAsync(request.Email);
        if (existing is not null)
        {
            if (existing.Status == UserStatus.Pending)
                throw new InviteAlreadyPendingException($"An invite for '{request.Email}' is already pending.");

            throw new InvalidOperationException($"Email '{request.Email}' is already registered.");
        }

        ApplicationUser user;
        try
        {
            user = await userRepository.CreatePendingUserAsync(
                request.FirstName, request.LastName, request.Email, request.CompanyId);

            foreach (string role in request.SystemRoles)
                await userRepository.AssignRoleAsync(user.Id, role);

            string rawToken = Guid.NewGuid().ToString("N");
            string tokenHash = HashHelper.Sha256(rawToken);
            DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddHours(ExpiryHours);
            await userRepository.StoreInviteTokenAsync(user.Id, tokenHash, expiresAt);

            string frontendBaseUrl = appUrlProvider.FrontendBaseUrl;
            string inviteUrl = $"{frontendBaseUrl.TrimEnd('/')}/accept-invite?token={Uri.EscapeDataString(rawToken)}";
            string body = await templateRenderer.RenderAsync("InviteUser", new Dictionary<string, string>
            {
                ["firstName"] = request.FirstName,
                ["inviteUrl"] = inviteUrl,
                ["expiryHours"] = ExpiryHours.ToString()
            });

            await emailService.SendAsync(request.Email, "You're invited to EasyLogin", body);
        }
        catch (Exception ex)
        {
            await auditLogger.WriteAsync(new AuditEntry
            {
                EventType = AuditEventType.UserCreateFailed,
                Success = false,
                TargetType = AuditTargetType.User,
                TargetDisplay = request.Email,
                FailureReason = ex.Message
            }, cancellationToken);
            throw;
        }

        (ApplicationUser detail, IList<string> systemRoles, IList<string> companyRoles) =
            await userRepository.GetByIdWithRolesAsync(user.Id);

        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.UserInvited,
            Success = true,
            TargetType = AuditTargetType.User,
            TargetId = user.Id,
            TargetDisplay = user.Email,
            Metadata = new Dictionary<string, string>
            {
                ["systemRoles"] = string.Join(',', systemRoles),
                ["companyId"] = request.CompanyId?.ToString() ?? string.Empty,
                ["expiresInHours"] = ExpiryHours.ToString()
            }
        }, cancellationToken);

        return new UserDetailResponse(
            detail.Id, detail.FirstName, detail.LastName, detail.Email,
            detail.IsActive, detail.CreatedAt, detail.UpdatedAt,
            detail.CompanyId, detail.CompanyName,
            systemRoles, companyRoles, detail.Status.ToString());
    }
}
