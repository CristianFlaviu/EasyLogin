using EasyLogin.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyLogin.Infrastructure.Persistence.Configurations;

public class AppIdentityRoleConfiguration : IEntityTypeConfiguration<AppIdentityRole>
{
    public void Configure(EntityTypeBuilder<AppIdentityRole> builder)
    {
        builder.Property(r => r.Description).HasMaxLength(500);
    }
}
