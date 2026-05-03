using EasyLogin.Application.Companies.Dtos;
using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Companies.Commands;

public record CreateCompanyRoleCommand(string Name, string? Description, Guid CompanyId) : IRequest<CompanyRoleResponse>;

public class CreateCompanyRoleCommandHandler(ICompanyRoleRepository companyRoleRepository)
    : IRequestHandler<CreateCompanyRoleCommand, CompanyRoleResponse>
{
    public Task<CompanyRoleResponse> Handle(CreateCompanyRoleCommand request, CancellationToken cancellationToken)
        => companyRoleRepository.CreateAsync(request.Name, request.Description, request.CompanyId);
}
