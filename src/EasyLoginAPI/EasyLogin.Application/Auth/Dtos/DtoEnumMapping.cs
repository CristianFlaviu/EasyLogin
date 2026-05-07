using EasyLogin.Application.Auth.Dtos.Enums;
using EasyLogin.Domain.Enums;

namespace EasyLogin.Application.Auth.Dtos;

public static class DtoEnumMapping
{
    public static UserStatusDto ToDto(this UserStatus status)
        => status switch
        {
            UserStatus.Active => UserStatusDto.Active,
            UserStatus.Pending => UserStatusDto.Pending,
            UserStatus.Suspended => UserStatusDto.Suspended,
            UserStatus.Expired => UserStatusDto.Expired,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

    public static UserStatus ToDomain(this UserStatusDto status)
        => status switch
        {
            UserStatusDto.Active => UserStatus.Active,
            UserStatusDto.Pending => UserStatus.Pending,
            UserStatusDto.Suspended => UserStatus.Suspended,
            UserStatusDto.Expired => UserStatus.Expired,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

    public static TwoFactorMethodDto ToDto(this TwoFactorMethod method)
        => method switch
        {
            TwoFactorMethod.Authenticator => TwoFactorMethodDto.Authenticator,
            TwoFactorMethod.Email => TwoFactorMethodDto.Email,
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };
}
