using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Auth.Queries;

public record ValidateInviteTokenQuery(string Token) : IRequest<InviteValidationResponse>;

public class ValidateInviteTokenQueryHandler(IUserRepository userRepository, IAuditLogger auditLogger)
    : IRequestHandler<ValidateInviteTokenQuery, InviteValidationResponse>
{
    public async Task<InviteValidationResponse> Handle(ValidateInviteTokenQuery request, CancellationToken cancellationToken)
    {
        try
        {
            string tokenHash = HashHelper.Sha256(request.Token);
            (string UserId, string Email, string FirstName, string LastName) invite =
                await userRepository.ValidateInviteTokenAsync(tokenHash);

            return new InviteValidationResponse(invite.Email, invite.FirstName, invite.LastName);
        }
        catch (Exception ex) when (ex is InviteTokenExpiredException or InviteTokenUsedException or KeyNotFoundException or InvalidOperationException)
        {
            await auditLogger.WriteAsync(new AuditEntry
            {
                EventType = AuditEventType.UserInviteValidateFailed,
                Success = false,
                TargetType = AuditTargetType.User,
                FailureReason = ex.Message
            }, cancellationToken);
            throw;
        }
    }
}
