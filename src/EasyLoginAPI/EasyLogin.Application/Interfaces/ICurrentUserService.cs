namespace EasyLogin.Application.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    Guid? CompanyId { get; }
}
