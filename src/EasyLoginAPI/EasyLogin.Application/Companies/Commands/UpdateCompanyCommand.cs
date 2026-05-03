using EasyLogin.Application.Companies.Dtos;
using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Companies.Commands;

public record UpdateCompanyCommand(Guid Id, string Name, bool IsActive) : IRequest<CompanyResponse>;

public class UpdateCompanyCommandHandler(ICompanyRepository companyRepository)
    : IRequestHandler<UpdateCompanyCommand, CompanyResponse>
{
    public Task<CompanyResponse> Handle(UpdateCompanyCommand request, CancellationToken cancellationToken)
        => companyRepository.UpdateAsync(request.Id, request.Name, request.IsActive);
}
