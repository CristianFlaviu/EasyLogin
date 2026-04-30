using EasyLogin.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyLogin.Infrastructure.Persistence.Configurations;

public class AppIdentityUserConfiguration : IEntityTypeConfiguration<AppIdentityUser>
{
    public void Configure(EntityTypeBuilder<AppIdentityUser> builder)
    {
        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.RefreshTokenHash).HasMaxLength(256);

        builder.HasIndex(u => u.RefreshTokenHash)
            .HasFilter("[RefreshTokenHash] IS NOT NULL");
    }
}
