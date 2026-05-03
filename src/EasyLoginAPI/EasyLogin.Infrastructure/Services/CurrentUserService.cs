using EasyLogin.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace EasyLogin.Infrastructure.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string? UserId =>
        httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
        ?? httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    public Guid? CompanyId
    {
        get
        {
            var val = httpContextAccessor.HttpContext?.User?.FindFirst("company_id")?.Value;
            return Guid.TryParse(val, out var id) ? id : null;
        }
    }
}
