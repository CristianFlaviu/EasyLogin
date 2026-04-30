using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record ResetPasswordCommand(string Email, string Token, string Password) : IRequest;

public class ResetPasswordCommandHandler(IUserRepository userRepository)
    : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        await userRepository.ResetPasswordAsync(request.Email, request.Token, request.Password);
    }
}
