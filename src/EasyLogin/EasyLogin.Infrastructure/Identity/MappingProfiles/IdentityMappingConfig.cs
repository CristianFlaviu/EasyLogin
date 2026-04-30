using EasyLogin.Domain.Entities;
using Mapster;

namespace EasyLogin.Infrastructure.Identity.MappingProfiles;

public class IdentityMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AppIdentityUser, ApplicationUser>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Email, src => src.Email ?? string.Empty);

        config.NewConfig<ApplicationUser, AppIdentityUser>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Email, src => src.Email)
            .Map(dest => dest.UserName, src => src.Email);

        config.NewConfig<AppIdentityRole, ApplicationRole>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name ?? string.Empty);

        config.NewConfig<ApplicationRole, AppIdentityRole>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name);
    }
}
