using EasyLogin.Application.Auth.Commands;
using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Auth.Queries;
using EasyLogin.Application.Companies.Commands;
using EasyLogin.Application.Companies.Dtos;
using EasyLogin.Application.Companies.Queries;
using EasyLogin.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyLoginAPI.Controllers;

[ApiController]
[Route("api/company")]
[Authorize(Roles = "CompanyAdmin")]
public class CompanyAdminController(IMediator mediator, ICurrentUserService currentUserService) : ControllerBase
{
    private Guid CallerCompanyId =>
        currentUserService.CompanyId
        ?? throw new UnauthorizedAccessException("CompanyAdmin has no company assigned.");

    // ── Users ────────────────────────────────────────────────────────────────

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        => Ok(await mediator.Send(new GetAllUsersQuery(pageNumber, pageSize, CallerCompanyId)));

    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUser(string id)
        => Ok(await mediator.Send(new GetUserByIdQuery(id, CallerCompanyId)));

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateCompanyUserRequest request)
    {
        var result = await mediator.Send(new CreateCompanyUserCommand(
            request.FirstName, request.LastName, request.Email, request.Password,
            request.CompanyRoleIds, CallerCompanyId));
        return StatusCode(201, result);
    }

    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateCompanyUserRequest request)
    {
        var result = await mediator.Send(new UpdateCompanyUserCommand(
            id, request.FirstName, request.LastName, request.Email,
            request.IsActive, request.CompanyRoleIds, request.NewPassword,
            CallerCompanyId));
        return Ok(result);
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        await mediator.Send(new DeleteUserCommand(id, CallerCompanyId));
        return NoContent();
    }

    // ── Company Roles ─────────────────────────────────────────────────────────

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
        => Ok(await mediator.Send(new GetCompanyRolesQuery(CallerCompanyId)));

    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole([FromBody] CreateCompanyRoleRequest request)
    {
        var result = await mediator.Send(new CreateCompanyRoleCommand(request.Name, request.Description, CallerCompanyId));
        return StatusCode(201, result);
    }

    [HttpDelete("roles/{id:guid}")]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        await mediator.Send(new DeleteCompanyRoleCommand(id, CallerCompanyId));
        return NoContent();
    }
}
