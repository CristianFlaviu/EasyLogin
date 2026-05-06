using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;

namespace EasyLogin.Application.Interfaces;

public interface IOverviewRepository
{
    Task<OverviewResponse> GetAsync(Guid? tenantId);
    Task<PaginatedList<OverviewLoginResponse>> GetLoginsLast24HoursAsync(Guid? tenantId, int pageNumber, int pageSize);
    Task<PaginatedList<OverviewActiveSessionResponse>> GetActiveSessionsAsync(Guid? tenantId, int pageNumber, int pageSize);
}
