using EasyLogin.Application.Auth.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyLoginAPI.Controllers;

[ApiController]
[Route("api/user")]
[Authorize]
public class UserController(IMediator mediator) : ControllerBase
{
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var result = await mediator.Send(new GetCurrentUserQuery());
        return Ok(result);
    }
}
