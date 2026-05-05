using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record DeleteUserCommand(string UserId, Guid? CallerCompanyId = null) : IRequest;

public class DeleteUserCommandHandler(IUserRepository userRepository, IAuditLogger auditLogger)
    : IRequestHandler<DeleteUserCommand>
{
    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        string? targetEmail = null;
        try
        {
            var existing = await userRepository.GetByIdAsync(request.UserId);
            targetEmail = existing.Email;
        }
        catch
        {
            // user lookup failure flows through DeleteUserAsync below
        }

        try
        {
            await userRepository.DeleteUserAsync(request.UserId, request.CallerCompanyId);
        }
        catch (Exception ex)
        {
            await auditLogger.WriteAsync(new AuditEntry
            {
                EventType = AuditEventType.UserDeleteFailed,
                Success = false,
                TargetType = AuditTargetType.User,
                TargetId = request.UserId,
                TargetDisplay = targetEmail,
                FailureReason = ex.Message
            }, cancellationToken);
            throw;
        }

        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.UserDeleted,
            Success = true,
            TargetType = AuditTargetType.User,
            TargetId = request.UserId,
            TargetDisplay = targetEmail
        }, cancellationToken);
    }
}
