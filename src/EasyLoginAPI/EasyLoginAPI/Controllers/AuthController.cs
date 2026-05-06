using EasyLogin.Application.Auth.Commands;
using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Auth.Queries;
using EasyLogin.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyLoginAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IMediator mediator, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var command = new RegisterCommand(request.FirstName, request.LastName, request.Email, request.Password);
        var result = await mediator.Send(command);
        return StatusCode(201, result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await mediator.Send(new LoginCommand(request.Email, request.Password));
        return Ok(result);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await mediator.Send(new ForgotPasswordCommand(request.Email));
        return Ok(new { message = "If an account with that email exists, a reset link has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await mediator.Send(new ResetPasswordCommand(request.Email, request.Token, request.Password));
        return Ok(new { message = "Password has been reset successfully." });
    }

    [HttpGet("invite/validate")]
    public async Task<IActionResult> ValidateInvite([FromQuery] string token)
        => Ok(await mediator.Send(new ValidateInviteTokenQuery(token)));

    [HttpPost("accept-invite")]
    public async Task<IActionResult> AcceptInvite([FromBody] AcceptInviteRequest request)
    {
        await mediator.Send(new AcceptInviteCommand(request.Token, request.Password, request.ConfirmPassword));
        return Ok(new { message = "Invite accepted." });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await mediator.Send(new RefreshTokenCommand(request.AccessToken, request.RefreshToken));
        return Ok(result);
    }

    [HttpPost("revoke")]
    [Authorize]
    public async Task<IActionResult> RevokeToken()
    {
        var userId = currentUserService.UserId;
        if (userId is null)
            return Unauthorized();

        await mediator.Send(new RevokeTokenCommand(userId));
        return Ok(new { message = "Token revoked." });
    }
}
