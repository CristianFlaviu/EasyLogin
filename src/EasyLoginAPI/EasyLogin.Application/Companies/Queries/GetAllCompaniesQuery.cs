using EasyLogin.Application.Companies.Dtos;
using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Companies.Queries;

public record GetAllCompaniesQuery : IRequest<IList<CompanyResponse>>;

public class GetAllCompaniesQueryHandler(ICompanyRepository companyRepository)
    : IRequestHandler<GetAllCompaniesQuery, IList<CompanyResponse>>
{
    public Task<IList<CompanyResponse>> Handle(GetAllCompaniesQuery request, CancellationToken cancellationToken)
        => companyRepository.GetAllAsync();
}
