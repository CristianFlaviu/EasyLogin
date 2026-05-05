using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Auth.Queries;

public record GetAuditLogsQuery(
    int PageNumber,
    int PageSize,
    string? ActorUserId = null,
    string? ActorEmail = null,
    string? TargetType = null,
    string? TargetId = null,
    string? EventType = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null) : IRequest<PaginatedList<AuditLogResponse>>;

public class GetAuditLogsQueryHandler(IAuditLogQueryRepository repository)
    : IRequestHandler<GetAuditLogsQuery, PaginatedList<AuditLogResponse>>
{
    private const int MaxPageSize = 100;

    public async Task<PaginatedList<AuditLogResponse>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        return await repository.GetPagedAsync(
            pageNumber, pageSize,
            request.ActorUserId, request.ActorEmail,
            request.TargetType, request.TargetId,
            request.EventType, request.From, request.To);
    }
}
