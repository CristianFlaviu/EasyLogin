namespace EasyLogin.Application.Common;

public class TwoFactorVerificationFailedException(string reason, bool isLockedOut = false)
    : Exception(isLockedOut
        ? "Too many failed verification attempts. Sign in again."
        : "Invalid password or verification code.")
{
    public string Reason { get; } = reason;
    public bool IsLockedOut { get; } = isLockedOut;
}
