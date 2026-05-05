using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;

namespace EasyLogin.Application.Interfaces;

public interface IAuditLogQueryRepository
{
    Task<PaginatedList<AuditLogResponse>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? actorUserId,
        string? actorEmail,
        string? targetType,
        string? targetId,
        string? eventType,
        DateTimeOffset? from,
        DateTimeOffset? to);
}
