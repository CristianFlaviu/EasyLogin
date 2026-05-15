using EasyLogin.Application.Notifications.Commands;
using EasyLogin.Application.Notifications.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyLoginAPI.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetMine(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        return Ok(await mediator.Send(new GetMyNotificationsQuery(unreadOnly, skip, take)));
    }

    [HttpGet("unread-count")]
    [Authorize]
    public async Task<IActionResult> GetUnreadCount()
    {
        return Ok(await mediator.Send(new GetUnreadCountQuery()));
    }

    [HttpPut("{id:guid}/read")]
    [Authorize]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        await mediator.Send(new MarkNotificationReadCommand(id));
        return Ok(new { message = "Notification marked as read." });
    }

    [HttpPut("read-all")]
    [Authorize]
    public async Task<IActionResult> MarkAllRead()
    {
        await mediator.Send(new MarkAllReadCommand());
        return Ok(new { message = "All notifications marked as read." });
    }

    [HttpPost("test/self")]
    [AllowAnonymous]
    public async Task<IActionResult> SendToSelf([FromBody] TestSelfNotificationRequest request)
    {
        int createdCount = await mediator.Send(new SendTestNotificationCommand(
            request.UserId,
            null,
            false,
            request.Title,
            request.Message,
            request.Type));

        return Ok(new { createdCount });
    }

    [HttpPost("test/user/{userId}")]
    [AllowAnonymous]
    public async Task<IActionResult> SendToUser(
        string userId,
        [FromBody] TestNotificationRequest request)
    {
        int createdCount = await mediator.Send(new SendTestNotificationCommand(
            userId,
            null,
            false,
            request.Title,
            request.Message,
            request.Type));

        return Ok(new { createdCount });
    }

    [HttpPost("test/tenant/{tenantId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> SendToTenant(
        Guid tenantId,
        [FromBody] TestNotificationRequest request)
    {
        int createdCount = await mediator.Send(new SendTestNotificationCommand(
            null,
            tenantId,
            false,
            request.Title,
            request.Message,
            request.Type));

        return Ok(new { createdCount });
    }

    [HttpPost("test/broadcast")]
    [AllowAnonymous]
    public async Task<IActionResult> Broadcast([FromBody] TestNotificationRequest request)
    {
        int createdCount = await mediator.Send(new SendTestNotificationCommand(
            null,
            null,
            true,
            request.Title,
            request.Message,
            request.Type));

        return Ok(new { createdCount });
    }
}

public record TestNotificationRequest(
    string Title,
    string Message,
    string Type);

public record TestSelfNotificationRequest(
    string UserId,
    string Title,
    string Message,
    string Type);
