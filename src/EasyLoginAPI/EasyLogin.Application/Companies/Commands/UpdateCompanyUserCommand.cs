using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Companies.Commands;

public record UpdateCompanyUserCommand(
    string UserId, string FirstName, string LastName, string Email,
    bool IsActive, IList<Guid> CompanyRoleIds, string? NewPassword,
    Guid CallerCompanyId)
    : IRequest<UserDetailResponse>;

public class UpdateCompanyUserCommandHandler(
    IUserRepository userRepository,
    ICompanyRoleRepository companyRoleRepository,
    IAuditLogger auditLogger)
    : IRequestHandler<UpdateCompanyUserCommand, UserDetailResponse>
{
    public async Task<UserDetailResponse> Handle(UpdateCompanyUserCommand request, CancellationToken cancellationToken)
    {
        var (before, _, beforeCompanyRoles) = await userRepository.GetByIdWithRolesAsync(request.UserId, request.CallerCompanyId);

        try
        {
            await userRepository.UpdateUserAsync(
                request.UserId, request.FirstName, request.LastName,
                request.Email, request.IsActive, null,
                request.NewPassword, request.CallerCompanyId);

            await companyRoleRepository.UpdateUserRolesAsync(
                request.UserId, request.CompanyRoleIds, request.CallerCompanyId);
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
        AuditDiffBuilder.ForCollection("companyRoles", beforeCompanyRoles, companyRoles, diff);
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
