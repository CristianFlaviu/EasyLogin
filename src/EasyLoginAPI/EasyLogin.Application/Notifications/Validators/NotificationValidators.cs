using EasyLogin.Application.Notifications.Commands;
using EasyLogin.Application.Notifications.Queries;
using FluentValidation;

namespace EasyLogin.Application.Notifications.Validators;

public class GetMyNotificationsQueryValidator : AbstractValidator<GetMyNotificationsQuery>
{
    public GetMyNotificationsQueryValidator()
    {
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Take).InclusiveBetween(1, 100);
    }
}

public class MarkNotificationReadCommandValidator : AbstractValidator<MarkNotificationReadCommand>
{
    public MarkNotificationReadCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class SendTestNotificationCommandValidator : AbstractValidator<SendTestNotificationCommand>
{
    public SendTestNotificationCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Type).NotEmpty().MaximumLength(64);
        RuleFor(x => x.TargetUserId)
            .NotEmpty()
            .When(x => !x.Broadcast && !x.TargetTenantId.HasValue);
        RuleFor(x => x.TargetTenantId)
            .Empty()
            .When(x => x.Broadcast);
    }
}
