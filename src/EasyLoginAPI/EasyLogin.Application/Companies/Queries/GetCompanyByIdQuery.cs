using EasyLogin.Application.Companies.Dtos;
using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Companies.Queries;

public record GetCompanyByIdQuery(Guid Id) : IRequest<CompanyResponse>;

public class GetCompanyByIdQueryHandler(ICompanyRepository companyRepository)
    : IRequestHandler<GetCompanyByIdQuery, CompanyResponse>
{
    public Task<CompanyResponse> Handle(GetCompanyByIdQuery request, CancellationToken cancellationToken)
        => companyRepository.GetByIdAsync(request.Id);
}
