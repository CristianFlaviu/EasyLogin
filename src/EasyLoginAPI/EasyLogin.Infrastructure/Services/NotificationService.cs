using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using EasyLogin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EasyLogin.Infrastructure.Services;

public class NotificationService(AppDbContext dbContext) : INotificationService
{
    public async Task<Notification> CreateAsync(
        string userId,
        string title,
        string message,
        string type,
        CancellationToken cancellationToken = default)
    {
        bool userExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId, cancellationToken);

        if (!userExists)
            throw new KeyNotFoundException("Target user was not found.");

        Notification notification = new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);

        return notification;
    }

    public async Task<List<Notification>> CreateForAllUsersAsync(
        string title,
        string message,
        string type,
        CancellationToken cancellationToken = default)
    {
        List<string> userIds = await dbContext.Users
            .AsNoTracking()
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        return await CreateForUsersAsync(userIds, title, message, type, cancellationToken);
    }

    public async Task<List<Notification>> CreateForTenantUsersAsync(
        Guid tenantId,
        string title,
        string message,
        string type,
        CancellationToken cancellationToken = default)
    {
        bool tenantExists = await dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.Id == tenantId, cancellationToken);

        if (!tenantExists)
            throw new KeyNotFoundException("Target tenant was not found.");

        List<string> userIds = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        return await CreateForUsersAsync(userIds, title, message, type, cancellationToken);
    }

    public async Task MarkReadAsync(
        Guid id,
        string userId,
        CancellationToken cancellationToken = default)
    {
        int updatedCount = await dbContext.Notifications
            .Where(n => n.Id == id && n.UserId == userId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(n => n.IsRead, true),
                cancellationToken);

        if (updatedCount == 0)
            throw new KeyNotFoundException("Notification was not found.");
    }

    public async Task MarkAllReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        await dbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(n => n.IsRead, true),
                cancellationToken);
    }

    public async Task<List<Notification>> GetForUserAsync(
        string userId,
        bool unreadOnly,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Notification> query = dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
    }

    private async Task<List<Notification>> CreateForUsersAsync(
        List<string> userIds,
        string title,
        string message,
        string type,
        CancellationToken cancellationToken)
    {
        DateTime createdAt = DateTime.UtcNow;
        List<Notification> notifications = userIds
            .Select(userId => new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                CreatedAt = createdAt
            })
            .ToList();

        if (notifications.Count == 0)
            return notifications;

        dbContext.Notifications.AddRange(notifications);
        await dbContext.SaveChangesAsync(cancellationToken);

        return notifications;
    }
}
