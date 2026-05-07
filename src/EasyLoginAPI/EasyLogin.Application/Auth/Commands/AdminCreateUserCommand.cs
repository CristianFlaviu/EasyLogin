using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record AdminCreateUserCommand(
    string FirstName, string LastName, string Email, string Password,
    IList<string> SystemRoles, Guid? TenantId)
    : IRequest<UserDetailResponse>;

public class AdminCreateUserCommandHandler(
    IUserRepository userRepository,
    IEmailService emailService,
    IEmailTemplateRenderer templateRenderer,
    IAuditLogger auditLogger)
    : IRequestHandler<AdminCreateUserCommand, UserDetailResponse>
{
    public async Task<UserDetailResponse> Handle(AdminCreateUserCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.EmailExistsAsync(request.Email))
        {
            await auditLogger.WriteAsync(new AuditEntry
            {
                EventType = AuditEventType.UserCreateFailed,
                Success = false,
                TargetType = AuditTargetType.User,
                TargetDisplay = request.Email,
                FailureReason = "EmailAlreadyRegistered"
            }, cancellationToken);
            throw new InvalidOperationException($"Email '{request.Email}' is already registered.");
        }

        Domain.Entities.ApplicationUser user;
        try
        {
            user = await userRepository.CreateUserAsync(
                request.FirstName, request.LastName, request.Email, request.Password, request.TenantId);

            foreach (var role in request.SystemRoles)
                await userRepository.AssignRoleAsync(user.Id, role);
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

        var body = await templateRenderer.RenderAsync("Welcome", new Dictionary<string, string>
        {
            ["firstName"] = request.FirstName,
            ["email"] = request.Email
        });
        await emailService.SendAsync(request.Email, "Welcome to EasyLogin", body);

        var (detail, systemRoles, tenantRoles) = await userRepository.GetByIdWithRolesAsync(user.Id);

        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.UserCreated,
            Success = true,
            TargetType = AuditTargetType.User,
            TargetId = user.Id,
            TargetDisplay = user.Email,
            Metadata = new Dictionary<string, string>
            {
                ["systemRoles"] = string.Join(',', systemRoles),
                ["tenantId"] = request.TenantId?.ToString() ?? string.Empty
            }
        }, cancellationToken);

        return new UserDetailResponse(
            detail.Id, detail.FirstName, detail.LastName, detail.Email,
            detail.CreatedAt, detail.UpdatedAt,
            detail.TenantId, detail.TenantName,
            systemRoles, tenantRoles, detail.Status.ToDto());
    }
}
