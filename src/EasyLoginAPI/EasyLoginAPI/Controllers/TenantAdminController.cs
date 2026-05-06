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
