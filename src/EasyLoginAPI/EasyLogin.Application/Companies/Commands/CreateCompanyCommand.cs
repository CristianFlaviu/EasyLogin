using EasyLogin.Application.Companies.Dtos;
using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Companies.Commands;

public record CreateCompanyCommand(string Name) : IRequest<CompanyResponse>;

public class CreateCompanyCommandHandler(ICompanyRepository companyRepository)
    : IRequestHandler<CreateCompanyCommand, CompanyResponse>
{
    public Task<CompanyResponse> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
        => companyRepository.CreateAsync(request.Name);
}
