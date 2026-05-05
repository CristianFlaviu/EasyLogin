using EasyLogin.Application.Common;

namespace EasyLogin.Application.Interfaces;

public interface IAuditLogger
{
    Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken = default);
}
