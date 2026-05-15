using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EasyLogin.Infrastructure.Realtime;

[Authorize]
public class NotificationHub : Hub
{
}
