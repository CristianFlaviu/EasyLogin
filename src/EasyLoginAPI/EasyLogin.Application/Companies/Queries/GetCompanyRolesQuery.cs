using EasyLogin.Application.Companies.Dtos;
using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Companies.Queries;

public record GetCompanyRolesQuery(Guid CompanyId) : IRequest<IList<CompanyRoleResponse>>;

public class GetCompanyRolesQueryHandler(ICompanyRoleRepository companyRoleRepository)
    : IRequestHandler<GetCompanyRolesQuery, IList<CompanyRoleResponse>>
{
    public Task<IList<CompanyRoleResponse>> Handle(GetCompanyRolesQuery request, CancellationToken cancellationToken)
        => companyRoleRepository.GetByCompanyIdAsync(request.CompanyId);
}
