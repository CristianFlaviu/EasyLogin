using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Companies.Commands;

public record DeleteCompanyCommand(Guid Id) : IRequest;

public class DeleteCompanyCommandHandler(ICompanyRepository companyRepository)
    : IRequestHandler<DeleteCompanyCommand>
{
    public Task Handle(DeleteCompanyCommand request, CancellationToken cancellationToken)
        => companyRepository.DeleteAsync(request.Id);
}
