using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EasyLogin.Infrastructure.Persistence;

public class AuditLogQueryRepository(AppDbContext db) : IAuditLogQueryRepository
{
    public async Task<PaginatedList<AuditLogResponse>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? actorUserId,
        string? actorEmail,
        string? targetType,
        string? targetId,
        string? eventType,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        var query = db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(actorUserId))
            query = query.Where(a => a.ActorUserId == actorUserId);

        if (!string.IsNullOrWhiteSpace(actorEmail))
            query = query.Where(a => a.ActorEmail == actorEmail);

        if (!string.IsNullOrWhiteSpace(targetType))
            query = query.Where(a => a.TargetType == targetType);

        if (!string.IsNullOrWhiteSpace(targetId))
            query = query.Where(a => a.TargetId == targetId);

        if (!string.IsNullOrWhiteSpace(eventType))
            query = query.Where(a => a.EventType == eventType);

        if (from.HasValue)
            query = query.Where(a => a.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.Timestamp <= to.Value);

        var total = await query.CountAsync();

        var rows = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogResponse(
                a.Id, a.Timestamp, a.EventType, a.Success,
                a.ActorUserId, a.ActorEmail,
                a.TargetType, a.TargetId, a.TargetDisplay,
                a.FailureReason,
                a.IpAddress, a.UserAgent,
                a.BrowserName, a.BrowserVersion,
                a.OsName, a.OsVersion, a.DeviceFamily,
                a.Jti, a.CorrelationId, a.MetadataJson))
            .ToListAsync();

        return new PaginatedList<AuditLogResponse>(rows, total, pageNumber, pageSize);
    }
}
