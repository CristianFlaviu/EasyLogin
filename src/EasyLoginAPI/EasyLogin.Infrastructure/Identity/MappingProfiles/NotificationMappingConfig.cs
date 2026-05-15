using EasyLogin.Application.Notifications.Dtos;
using EasyLogin.Domain.Entities;
using Mapster;

namespace EasyLogin.Infrastructure.Identity.MappingProfiles;

public class NotificationMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Notification, NotificationResponse>();
    }
}
