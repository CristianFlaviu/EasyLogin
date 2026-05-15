using EasyLogin.Domain.Entities;

namespace EasyLogin.Application.Interfaces;

public interface INotificationService
{
    Task<Notification> CreateAsync(
        string userId,
        string title,
        string message,
        string type,
        CancellationToken cancellationToken = default);

    Task<List<Notification>> CreateForAllUsersAsync(
        string title,
        string message,
        string type,
        CancellationToken cancellationToken = default);

    Task<List<Notification>> CreateForTenantUsersAsync(
        Guid tenantId,
        string title,
        string message,
        string type,
        CancellationToken cancellationToken = default);

    Task MarkReadAsync(Guid id, string userId, CancellationToken cancellationToken = default);

    Task MarkAllReadAsync(string userId, CancellationToken cancellationToken = default);

    Task<List<Notification>> GetForUserAsync(
        string userId,
        bool unreadOnly,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);
}
