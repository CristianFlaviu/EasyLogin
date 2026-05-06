using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Auth.Commands;
using EasyLogin.Application.Tenants.Commands;
using FluentValidation;

namespace EasyLogin.Application.Auth.Validators;

public class InviteUserRequestValidator : AbstractValidator<InviteUserRequest>
{
    public InviteUserRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public class InviteUserCommandValidator : AbstractValidator<InviteUserCommand>
{
    public InviteUserCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public class InviteTenantUserRequestValidator : AbstractValidator<InviteTenantUserRequest>
{
    public InviteTenantUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.TenantRoleId).NotEmpty();
    }
}

public class InviteTenantUserCommandValidator : AbstractValidator<InviteTenantUserCommand>
{
    public InviteTenantUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.TenantRoleId).NotEmpty();
    }
}
