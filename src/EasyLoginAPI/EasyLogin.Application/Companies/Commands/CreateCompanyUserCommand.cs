using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Companies.Commands;

public record CreateCompanyUserCommand(
    string FirstName, string LastName, string Email, string Password,
    IList<Guid> CompanyRoleIds, Guid CallerCompanyId)
    : IRequest<UserDetailResponse>;

public class CreateCompanyUserCommandHandler(
    IUserRepository userRepository,
    ICompanyRoleRepository companyRoleRepository,
    IEmailService emailService,
    IEmailTemplateRenderer templateRenderer,
    IAuditLogger auditLogger)
    : IRequestHandler<CreateCompanyUserCommand, UserDetailResponse>
{
    public async Task<UserDetailResponse> Handle(CreateCompanyUserCommand request, CancellationToken cancellationToken)
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
                request.FirstName, request.LastName, request.Email, request.Password, request.CallerCompanyId);

            if (request.CompanyRoleIds.Count > 0)
                await companyRoleRepository.UpdateUserRolesAsync(user.Id, request.CompanyRoleIds, request.CallerCompanyId);
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

        var (detail, systemRoles, companyRoles) = await userRepository.GetByIdWithRolesAsync(user.Id, request.CallerCompanyId);

        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.UserCreated,
            Success = true,
            TargetType = AuditTargetType.User,
            TargetId = user.Id,
            TargetDisplay = user.Email,
            Metadata = new Dictionary<string, string>
            {
                ["companyId"] = request.CallerCompanyId.ToString(),
                ["companyRoles"] = string.Join(',', companyRoles)
            }
        }, cancellationToken);

        return new UserDetailResponse(
            detail.Id, detail.FirstName, detail.LastName, detail.Email,
            detail.IsActive, detail.CreatedAt, detail.UpdatedAt,
            detail.CompanyId, detail.CompanyName,
            systemRoles, companyRoles, detail.Status.ToString());
    }
}
