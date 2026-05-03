using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record DeleteUserCommand(string UserId, Guid? CallerCompanyId = null) : IRequest;

public class DeleteUserCommandHandler(IUserRepository userRepository)
    : IRequestHandler<DeleteUserCommand>
{
    public Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        => userRepository.DeleteUserAsync(request.UserId, request.CallerCompanyId);
}
