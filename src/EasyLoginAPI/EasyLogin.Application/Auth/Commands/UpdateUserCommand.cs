using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record UpdateUserCommand(
    string UserId, string FirstName, string LastName, string Email,
    bool IsActive, IList<string> SystemRoles, string? NewPassword,
    Guid? CallerCompanyId = null)
    : IRequest<UserDetailResponse>;

public class UpdateUserCommandHandler(IUserRepository userRepository, IAuditLogger auditLogger)
    : IRequestHandler<UpdateUserCommand, UserDetailResponse>
{
    public async Task<UserDetailResponse> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var (before, beforeSysRoles, _) = await userRepository.GetByIdWithRolesAsync(request.UserId, request.CallerCompanyId);

        try
        {
            await userRepository.UpdateUserAsync(
                request.UserId, request.FirstName, request.LastName,
                request.Email, request.IsActive, request.SystemRoles,
                request.NewPassword, request.CallerCompanyId);
        }
        catch (Exception ex)
        {
            await auditLogger.WriteAsync(new AuditEntry
            {
                EventType = AuditEventType.UserUpdateFailed,
                Success = false,
                TargetType = AuditTargetType.User,
                TargetId = request.UserId,
                TargetDisplay = before.Email,
                FailureReason = ex.Message
            }, cancellationToken);
            throw;
        }

        var (user, systemRoles, companyRoles) = await userRepository.GetByIdWithRolesAsync(request.UserId, request.CallerCompanyId);

        var diff = new Dictionary<string, string>();
        AuditDiffBuilder.ForField("firstName", before.FirstName, user.FirstName, diff);
        AuditDiffBuilder.ForField("lastName", before.LastName, user.LastName, diff);
        AuditDiffBuilder.ForField("email", before.Email, user.Email, diff);
        AuditDiffBuilder.ForField("isActive", before.IsActive, user.IsActive, diff);
        AuditDiffBuilder.ForCollection("systemRoles", beforeSysRoles, systemRoles, diff);
        if (!string.IsNullOrWhiteSpace(request.NewPassword))
            diff["password"] = "changed";

        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.UserUpdated,
            Success = true,
            TargetType = AuditTargetType.User,
            TargetId = user.Id,
            TargetDisplay = user.Email,
            Metadata = diff.Count > 0 ? diff : null
        }, cancellationToken);

        return new UserDetailResponse(
            user.Id, user.FirstName, user.LastName, user.Email,
            user.IsActive, user.CreatedAt, user.UpdatedAt,
            user.CompanyId, user.CompanyName,
            systemRoles, companyRoles);
    }
}
