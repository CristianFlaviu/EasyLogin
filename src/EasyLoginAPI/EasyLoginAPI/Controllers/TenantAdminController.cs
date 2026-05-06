using EasyLogin.Application.Auth.Commands;
using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Auth.Queries;
using EasyLogin.Application.Tenants.Commands;
using EasyLogin.Application.Tenants.Dtos;
using EasyLogin.Application.Tenants.Queries;
using EasyLogin.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyLoginAPI.Controllers;

[ApiController]
[Route("api/tenant")]
[Authorize(Roles = "TenantAdmin")]
public class TenantAdminController(IMediator mediator, ICurrentUserService currentUserService) : ControllerBase
{
    private Guid CallerTenantId =>
        currentUserService.TenantId
        ?? throw new UnauthorizedAccessException("TenantAdmin has no tenant assigned.");

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
        => Ok(await mediator.Send(new GetOverviewQuery(CallerTenantId)));

    [HttpGet("overview/logins")]
    public async Task<IActionResult> GetOverviewLogins(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
        => Ok(await mediator.Send(new GetOverviewLoginsQuery(CallerTenantId, pageNumber, pageSize)));

    [HttpGet("overview/sessions")]
    public async Task<IActionResult> GetOverviewActiveSessions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
        => Ok(await mediator.Send(new GetOverviewActiveSessionsQuery(CallerTenantId, pageNumber, pageSize)));

    // ── Users ────────────────────────────────────────────────────────────────

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        => Ok(await mediator.Send(new GetAllUsersQuery(pageNumber, pageSize, CallerTenantId)));

    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUser(string id)
        => Ok(await mediator.Send(new GetUserByIdQuery(id, CallerTenantId)));

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateTenantUserRequest request)
    {
        var result = await mediator.Send(new CreateTenantUserCommand(
            request.FirstName, request.LastName, request.Email, request.Password,
            request.TenantRoleIds, CallerTenantId));
        return StatusCode(201, result);
    }

    [HttpPost("users/invite")]
    public async Task<IActionResult> InviteUser([FromBody] InviteTenantUserRequest request)
    {
        var result = await mediator.Send(new InviteTenantUserCommand(
            request.Email, request.TenantRoleId, CallerTenantId));
        return StatusCode(201, result);
    }

    [HttpPost("users/{id}/resend-invite")]
    public async Task<IActionResult> ResendInvite(string id)
    {
        await mediator.Send(new ResendTenantInviteCommand(id, CallerTenantId));
        return Ok(new { message = "Invite resent." });
    }

    [HttpPost("users/{id}/revoke-invite")]
    public async Task<IActionResult> RevokeInvite(string id)
    {
        await mediator.Send(new RevokeTenantInviteCommand(id, CallerTenantId));
        return Ok(new { message = "Invite revoked." });
    }

    [HttpPost("users/{id}/suspend")]
    public async Task<IActionResult> SuspendUser(string id)
    {
        await mediator.Send(new SuspendTenantUserCommand(id, CallerTenantId, currentUserService.UserId));
        return Ok(new { message = "User suspended." });
    }

    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateTenantUserRequest request)
    {
        var result = await mediator.Send(new UpdateTenantUserCommand(
            id, request.FirstName, request.LastName, request.Email,
            request.IsActive, request.TenantRoleIds, request.NewPassword,
            CallerTenantId));
        return Ok(result);
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        await mediator.Send(new DeleteUserCommand(id, CallerTenantId));
        return NoContent();
    }

    // ── Tenant Roles ─────────────────────────────────────────────────────────

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
        => Ok(await mediator.Send(new GetTenantRolesQuery(CallerTenantId)));

    [HttpGet("context")]
    public async Task<IActionResult> GetContext()
        => Ok(await mediator.Send(new GetTenantByIdQuery(CallerTenantId)));

    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole([FromBody] CreateTenantRoleRequest request)
    {
        var result = await mediator.Send(new CreateTenantRoleCommand(request.Name, request.Description, CallerTenantId));
        return StatusCode(201, result);
    }

    [HttpDelete("roles/{id:guid}")]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        await mediator.Send(new DeleteTenantRoleCommand(id, CallerTenantId));
        return NoContent();
    }
}
