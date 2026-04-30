using EasyLogin.Application.Auth.Dtos;
using FluentValidation;

namespace EasyLogin.Application.Auth.Validators;

public class RevokeTokenRequestValidator : AbstractValidator<RevokeTokenRequest>
{
    public RevokeTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
