using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Auth.Queries;

public record GetOverviewQuery(Guid? TenantId = null) : IRequest<OverviewResponse>;

public class GetOverviewQueryHandler(IOverviewRepository repository)
    : IRequestHandler<GetOverviewQuery, OverviewResponse>
{
    public Task<OverviewResponse> Handle(GetOverviewQuery request, CancellationToken cancellationToken)
        => repository.GetAsync(request.TenantId);
}

public record GetOverviewLoginsQuery(Guid? TenantId, int PageNumber, int PageSize)
    : IRequest<PaginatedList<OverviewLoginResponse>>;

public class GetOverviewLoginsQueryHandler(IOverviewRepository repository)
    : IRequestHandler<GetOverviewLoginsQuery, PaginatedList<OverviewLoginResponse>>
{
    private const int MaxPageSize = 100;

    public Task<PaginatedList<OverviewLoginResponse>> Handle(
        GetOverviewLoginsQuery request,
        CancellationToken cancellationToken)
    {
        int pageNumber = Math.Max(1, request.PageNumber);
        int pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        return repository.GetLoginsLast24HoursAsync(request.TenantId, pageNumber, pageSize);
    }
}

public record GetOverviewActiveSessionsQuery(Guid? TenantId, int PageNumber, int PageSize)
    : IRequest<PaginatedList<OverviewActiveSessionResponse>>;

public class GetOverviewActiveSessionsQueryHandler(IOverviewRepository repository)
    : IRequestHandler<GetOverviewActiveSessionsQuery, PaginatedList<OverviewActiveSessionResponse>>
{
    private const int MaxPageSize = 100;

    public Task<PaginatedList<OverviewActiveSessionResponse>> Handle(
        GetOverviewActiveSessionsQuery request,
        CancellationToken cancellationToken)
    {
        int pageNumber = Math.Max(1, request.PageNumber);
        int pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        return repository.GetActiveSessionsAsync(request.TenantId, pageNumber, pageSize);
    }
}
