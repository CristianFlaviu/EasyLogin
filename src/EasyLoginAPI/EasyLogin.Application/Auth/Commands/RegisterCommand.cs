using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record RegisterCommand(string FirstName, string LastName, string Email, string Password) : IRequest<AuthResponse>;

public class RegisterCommandHandler(
    IUserRepository userRepository,
    ITokenService tokenService,
    IEmailService emailService,
    IEmailTemplateRenderer templateRenderer)
    : IRequestHandler<RegisterCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.EmailExistsAsync(request.Email))
            throw new InvalidOperationException($"Email '{request.Email}' is already registered.");

        var user = await userRepository.CreateUserAsync(
            request.FirstName, request.LastName, request.Email, request.Password);

        await userRepository.AssignRoleAsync(user.Id, "User");

        var roles = new List<string> { "User" };
        var accessToken = tokenService.GenerateAccessToken(user, roles);
        var rawRefreshToken = tokenService.GenerateRefreshToken();
        var expiry = DateTimeOffset.UtcNow.AddDays(7);

        await userRepository.StoreRefreshTokenAsync(user.Id, HashHelper.Sha256(rawRefreshToken), expiry);

        var body = await templateRenderer.RenderAsync("Welcome", new Dictionary<string, string>
        {
            ["firstName"] = request.FirstName,
            ["email"] = request.Email
        });
        await emailService.SendAsync(request.Email, "Welcome to EasyLogin", body);

        return new AuthResponse(accessToken, rawRefreshToken, tokenService.AccessTokenExpiryMinutes * 60);
    }
}
