using EasyLogin.Application.Auth.Commands;
using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Auth.Queries;
using EasyLogin.Application.Companies.Commands;
using EasyLogin.Application.Companies.Dtos;
using EasyLogin.Application.Companies.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyLoginAPI.Controllers;

[ApiController]
[Route("api/superadmin")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminController(IMediator mediator) : ControllerBase
{
    // ── Users ────────────────────────────────────────────────────────────────

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        => Ok(await mediator.Send(new GetAllUsersQuery(pageNumber, pageSize)));

    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUser(string id)
        => Ok(await mediator.Send(new GetUserByIdQuery(id)));

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserRequest request)
    {
        var result = await mediator.Send(new AdminCreateUserCommand(
            request.FirstName, request.LastName, request.Email, request.Password,
            request.SystemRoles, request.CompanyId));
        return StatusCode(201, result);
    }

    [HttpPost("users/invite")]
    public async Task<IActionResult> InviteUser([FromBody] InviteUserRequest request)
    {
        UserDetailResponse result = await mediator.Send(new InviteUserCommand(
            request.FirstName, request.LastName, request.Email,
            request.SystemRoles, request.CompanyId));
        return StatusCode(201, result);
    }

    [HttpPost("users/{id}/resend-invite")]
    public async Task<IActionResult> ResendInvite(string id)
    {
        await mediator.Send(new ResendInviteCommand(id));
        return Ok(new { message = "Invite resent." });
    }

    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request)
    {
        var result = await mediator.Send(new UpdateUserCommand(
            id, request.FirstName, request.LastName, request.Email,
            request.IsActive, request.SystemRoles, request.NewPassword));
        return Ok(result);
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        await mediator.Send(new DeleteUserCommand(id));
        return NoContent();
    }

    // ── System Roles ─────────────────────────────────────────────────────────

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
        => Ok(await mediator.Send(new GetAllRolesQuery()));

    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        var result = await mediator.Send(new CreateRoleCommand(request.Name, request.Description));
        return StatusCode(201, result);
    }

    [HttpDelete("roles/{id}")]
    public async Task<IActionResult> DeleteRole(string id)
    {
        await mediator.Send(new DeleteRoleCommand(id));
        return NoContent();
    }

    // ── Companies ─────────────────────────────────────────────────────────────

    [HttpGet("companies")]
    public async Task<IActionResult> GetCompanies()
        => Ok(await mediator.Send(new GetAllCompaniesQuery()));

    [HttpGet("companies/{id:guid}")]
    public async Task<IActionResult> GetCompany(Guid id)
        => Ok(await mediator.Send(new GetCompanyByIdQuery(id)));

    [HttpPost("companies")]
    public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyRequest request)
    {
        var result = await mediator.Send(new CreateCompanyCommand(request.Name));
        return StatusCode(201, result);
    }

    [HttpPut("companies/{id:guid}")]
    public async Task<IActionResult> UpdateCompany(Guid id, [FromBody] UpdateCompanyRequest request)
        => Ok(await mediator.Send(new UpdateCompanyCommand(id, request.Name, request.IsActive)));

    [HttpDelete("companies/{id:guid}")]
    public async Task<IActionResult> DeleteCompany(Guid id)
    {
        await mediator.Send(new DeleteCompanyCommand(id));
        return NoContent();
    }

    [HttpGet("companies/{id:guid}/users")]
    public async Task<IActionResult> GetCompanyUsers(
        Guid id, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        => Ok(await mediator.Send(new GetAllUsersQuery(pageNumber, pageSize, id)));

    [HttpGet("companies/{id:guid}/roles")]
    public async Task<IActionResult> GetCompanyRoles(Guid id)
        => Ok(await mediator.Send(new GetCompanyRolesQuery(id)));

    // ── Audit ────────────────────────────────────────────────────────────────

    [HttpGet("audit")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? actorUserId = null,
        [FromQuery] string? actorEmail = null,
        [FromQuery] string? targetType = null,
        [FromQuery] string? targetId = null,
        [FromQuery] string? eventType = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null)
        => Ok(await mediator.Send(new GetAuditLogsQuery(
            pageNumber, pageSize, actorUserId, actorEmail, targetType, targetId, eventType, from, to)));
}
