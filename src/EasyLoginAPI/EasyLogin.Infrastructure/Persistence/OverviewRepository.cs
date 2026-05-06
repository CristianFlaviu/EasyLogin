using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using EasyLogin.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace EasyLogin.Infrastructure.Persistence;

public class OverviewRepository(AppDbContext db) : IOverviewRepository
{
    public async Task<OverviewResponse> GetAsync(Guid? tenantId)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset since = now.AddHours(-24);

        IQueryable<AppIdentityUser> scopedUsers = BuildScopedUsersQuery(tenantId);
        IQueryable<string> scopedUserIds = scopedUsers.Select(user => user.Id);

        int totalUsers = await scopedUsers.CountAsync();

        int activeSessions = await scopedUsers
            .CountAsync(user => user.RefreshTokenHash != null && user.RefreshTokenExpiry > now);

        IQueryable<AuditLog> loginQuery = db.AuditLogs
            .AsNoTracking()
            .Where(log =>
                log.EventType == AuditEventType.LoginSuccess
                && log.Success
                && log.Timestamp >= since);

        if (tenantId.HasValue)
        {
            loginQuery = loginQuery.Where(log =>
                log.ActorUserId != null && scopedUserIds.Contains(log.ActorUserId));
        }

        int loginsLast24Hours = await loginQuery.CountAsync();

        return new OverviewResponse(totalUsers, loginsLast24Hours, activeSessions);
    }

    public async Task<PaginatedList<OverviewLoginResponse>> GetLoginsLast24HoursAsync(
        Guid? tenantId,
        int pageNumber,
        int pageSize)
    {
        DateTimeOffset since = DateTimeOffset.UtcNow.AddHours(-24);
        IQueryable<string> scopedUserIds = BuildScopedUsersQuery(tenantId).Select(user => user.Id);

        IQueryable<AuditLog> query = db.AuditLogs
            .AsNoTracking()
            .Where(log =>
                log.EventType == AuditEventType.LoginSuccess
                && log.Success
                && log.Timestamp >= since);

        if (tenantId.HasValue)
        {
            query = query.Where(log =>
                log.ActorUserId != null && scopedUserIds.Contains(log.ActorUserId));
        }

        int total = await query.CountAsync();

        List<OverviewLoginResponse> rows = await query
            .OrderByDescending(log => log.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(log => new OverviewLoginResponse(
                log.Id,
                log.Timestamp,
                log.ActorUserId,
                log.ActorEmail,
                log.IpAddress,
                log.BrowserName,
                log.OsName,
                log.DeviceFamily))
            .ToListAsync();

        return new PaginatedList<OverviewLoginResponse>(rows, total, pageNumber, pageSize);
    }

    public async Task<PaginatedList<OverviewActiveSessionResponse>> GetActiveSessionsAsync(
        Guid? tenantId,
        int pageNumber,
        int pageSize)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        IQueryable<OverviewActiveSessionResponse> query =
            from user in BuildScopedUsersQuery(tenantId)
            join tenant in db.Tenants.AsNoTracking() on user.TenantId equals tenant.Id into tenantGroup
            from tenant in tenantGroup.DefaultIfEmpty()
            where user.RefreshTokenHash != null && user.RefreshTokenExpiry > now
            orderby user.RefreshTokenExpiry descending
            select new OverviewActiveSessionResponse(
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email ?? string.Empty,
                user.TenantId,
                tenant == null ? null : tenant.Name,
                user.RefreshTokenExpiry);

        int total = await query.CountAsync();

        List<OverviewActiveSessionResponse> rows = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedList<OverviewActiveSessionResponse>(rows, total, pageNumber, pageSize);
    }

    private IQueryable<AppIdentityUser> BuildScopedUsersQuery(Guid? tenantId)
    {
        IQueryable<AppIdentityUser> query = db.Users.AsNoTracking();

        if (!tenantId.HasValue)
            return query;

        Guid requiredTenantId = tenantId.Value;

        return query.Where(user =>
            user.TenantId == requiredTenantId
            || db.UserTenantRoles
                .Where(userTenantRole => userTenantRole.UserId == user.Id)
                .Join(
                    db.TenantRoles,
                    userTenantRole => userTenantRole.TenantRoleId,
                    tenantRole => tenantRole.Id,
                    (_, tenantRole) => tenantRole.TenantId)
                .Any(tenantRoleTenantId => tenantRoleTenantId == requiredTenantId));
    }
}
