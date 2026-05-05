using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using EasyLogin.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using UAParser;

namespace EasyLogin.Infrastructure.Services;

public class AuditLogger(AppDbContext db, IHttpContextAccessor httpContextAccessor) : IAuditLogger
{
    private static readonly Parser UaParser = Parser.GetDefault();

    public async Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        var http = httpContextAccessor.HttpContext;
        var ip = ResolveIp(http);
        var userAgent = http?.Request.Headers.UserAgent.ToString();
        var correlationId = http?.TraceIdentifier;

        var (claimUserId, claimEmail) = ReadActorClaims(http);
        var actorUserId = entry.ActorUserId ?? claimUserId;
        var actorEmail = entry.ActorEmail ?? claimEmail;

        string? browserName = null, browserVersion = null, osName = null, osVersion = null, deviceFamily = null;
        if (!string.IsNullOrWhiteSpace(userAgent))
        {
            var parsed = UaParser.Parse(userAgent);
            browserName = parsed.UA.Family;
            browserVersion = JoinVersion(parsed.UA.Major, parsed.UA.Minor, parsed.UA.Patch);
            osName = parsed.OS.Family;
            osVersion = JoinVersion(parsed.OS.Major, parsed.OS.Minor, parsed.OS.Patch);
            deviceFamily = parsed.Device.Family;
        }

        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            EventType = entry.EventType,
            Success = entry.Success,
            ActorUserId = actorUserId,
            ActorEmail = actorEmail,
            TargetType = entry.TargetType,
            TargetId = entry.TargetId,
            TargetDisplay = entry.TargetDisplay,
            FailureReason = entry.FailureReason,
            IpAddress = ip,
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent,
            BrowserName = browserName,
            BrowserVersion = browserVersion,
            OsName = osName,
            OsVersion = osVersion,
            DeviceFamily = deviceFamily,
            Jti = entry.Jti,
            CorrelationId = correlationId,
            MetadataJson = entry.Metadata is { Count: > 0 } ? JsonSerializer.Serialize(entry.Metadata) : null
        };

        db.AuditLogs.Add(log);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static (string? UserId, string? Email) ReadActorClaims(HttpContext? http)
    {
        var user = http?.User;
        if (user?.Identity is null || !user.Identity.IsAuthenticated) return (null, null);

        var userId = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = user.FindFirst(JwtRegisteredClaimNames.Email)?.Value
            ?? user.FindFirst(ClaimTypes.Email)?.Value;
        return (userId, email);
    }

    private static string? ResolveIp(HttpContext? http)
    {
        if (http is null) return null;

        var fwd = http.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(fwd))
        {
            var first = fwd.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first)) return first;
        }

        return http.Connection.RemoteIpAddress?.ToString();
    }

    private static string? JoinVersion(string? major, string? minor, string? patch)
    {
        var parts = new[] { major, minor, patch }.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
        return parts.Length == 0 ? null : string.Join('.', parts);
    }
}
