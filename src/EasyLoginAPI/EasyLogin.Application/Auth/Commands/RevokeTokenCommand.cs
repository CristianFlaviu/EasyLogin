using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record RevokeTokenCommand(string UserId) : IRequest;

public class RevokeTokenCommandHandler(IUserRepository userRepository)
    : IRequestHandler<RevokeTokenCommand>
{
    public async Task Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        await userRepository.RevokeRefreshTokenAsync(request.UserId);
    }
}
