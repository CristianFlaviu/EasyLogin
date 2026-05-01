using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record AdminCreateUserCommand(string FirstName, string LastName, string Email, string Password, IList<string> Roles)
    : IRequest<UserDetailResponse>;

public class AdminCreateUserCommandHandler(IUserRepository userRepository)
    : IRequestHandler<AdminCreateUserCommand, UserDetailResponse>
{
    public async Task<UserDetailResponse> Handle(AdminCreateUserCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.EmailExistsAsync(request.Email))
            throw new InvalidOperationException($"Email '{request.Email}' is already registered.");

        var user = await userRepository.CreateUserAsync(request.FirstName, request.LastName, request.Email, request.Password);

        var roles = request.Roles.Count > 0 ? request.Roles : new List<string> { "User" };
        foreach (var role in roles)
            await userRepository.AssignRoleAsync(user.Id, role);

        return new UserDetailResponse(user.Id, user.FirstName, user.LastName, user.Email, user.IsActive, user.CreatedAt, roles);
    }
}
