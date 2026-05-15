namespace EasyLogin.Application.Notifications.Dtos;

public record NotificationResponse(
    Guid Id,
    string Title,
    string Message,
    string Type,
    bool IsRead,
    DateTime CreatedAt);
