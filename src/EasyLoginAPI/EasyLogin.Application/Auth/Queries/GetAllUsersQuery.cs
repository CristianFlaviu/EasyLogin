using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Auth.Queries;

public record GetAllUsersQuery(int PageNumber, int PageSize, Guid? CompanyId = null)
    : IRequest<PaginatedList<UserListItemResponse>>;

public class GetAllUsersQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetAllUsersQuery, PaginatedList<UserListItemResponse>>
{
    private const int MaxPageSize = 100;

    public async Task<PaginatedList<UserListItemResponse>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Min(request.PageSize, MaxPageSize);
        return await userRepository.GetPagedUsersAsync(request.PageNumber, pageSize, request.CompanyId);
    }
}
