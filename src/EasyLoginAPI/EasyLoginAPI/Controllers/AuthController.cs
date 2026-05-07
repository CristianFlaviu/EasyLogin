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
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await mediator.Send(new LoginCommand(request.Email, request.Password));
        return Ok(result);
    }

    [HttpPost("login/verify-2fa")]
    public async Task<IActionResult> VerifyTwoFactor([FromBody] VerifyTwoFactorRequest request)
    {
        var result = await mediator.Send(new VerifyTwoFactorCommand(
            request.TwoFactorToken,
            request.Code));
        return Ok(result);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await mediator.Send(new ForgotPasswordCommand(request.Email));
        return Ok(new { message = "If an account with that email exists, a reset link has been sent." });
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
    {
        await mediator.Send(new ConfirmEmailCommand(request.Email, request.Token));
        return Ok(new { message = "Email confirmed." });
    }

    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendEmailConfirmation([FromBody] ResendEmailConfirmationRequest request)
    {
        await mediator.Send(new ResendEmailConfirmationCommand(request.Email));
        return Ok(new { message = "If the account needs confirmation, a confirmation email has been sent." });
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

    [HttpPost("2fa/enable")]
    [Authorize]
    public async Task<IActionResult> EnableTwoFactor([FromBody] EnableTwoFactorRequest request)
    {
        string? userId = currentUserService.UserId;
        if (userId is null)
            return Unauthorized();

        TwoFactorSetupResponse result = await mediator.Send(new EnableTwoFactorCommand(userId, request.Password));
        return Ok(result);
    }

    [HttpPost("2fa/confirm")]
    [Authorize]
    public async Task<IActionResult> ConfirmTwoFactor([FromBody] ConfirmTwoFactorRequest request)
    {
        string? userId = currentUserService.UserId;
        if (userId is null)
            return Unauthorized();

        await mediator.Send(new ConfirmTwoFactorCommand(userId, request.Code));
        return Ok(new { message = "Two-factor authentication enabled." });
    }

    [HttpPost("2fa/email/enable")]
    [Authorize]
    public async Task<IActionResult> EnableEmailTwoFactor([FromBody] EnableEmailTwoFactorRequest request)
    {
        string? userId = currentUserService.UserId;
        if (userId is null)
            return Unauthorized();

        await mediator.Send(new EnableEmailTwoFactorCommand(userId, request.Password));
        return Ok(new { message = "Email two-factor authentication enabled." });
    }

    [HttpPost("2fa/email/send-code")]
    [Authorize]
    public async Task<IActionResult> SendEmailTwoFactorCode()
    {
        string? userId = currentUserService.UserId;
        if (userId is null)
            return Unauthorized();

        await mediator.Send(new SendEmailTwoFactorCodeCommand(userId));
        return Ok(new { message = "Verification code sent." });
    }

    [HttpPost("2fa/disable")]
    [Authorize]
    public async Task<IActionResult> DisableTwoFactor([FromBody] DisableTwoFactorRequest request)
    {
        string? userId = currentUserService.UserId;
        if (userId is null)
            return Unauthorized();

        await mediator.Send(new DisableTwoFactorCommand(
            userId,
            request.Password,
            request.Code));
        return Ok(new { message = "Two-factor authentication disabled." });
    }
}
