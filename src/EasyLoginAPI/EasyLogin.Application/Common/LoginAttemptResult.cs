using EasyLogin.Domain.Entities;

namespace EasyLogin.Application.Common;

public record LoginAttemptResult(
    bool Success,
    string? FailureReason,
    ApplicationUser? User,
    IList<string>? Roles)
{
    public static LoginAttemptResult Failed(string reason) => new(false, reason, null, null);
    public static LoginAttemptResult Ok(ApplicationUser user, IList<string> roles) => new(true, null, user, roles);
}
