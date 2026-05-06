using EasyLogin.Domain.Entities;
using EasyLogin.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyLogin.Infrastructure.Persistence.Configurations;

public class TenantRoleConfiguration : IEntityTypeConfiguration<TenantRole>
{
    public void Configure(EntityTypeBuilder<TenantRole> builder)
    {
        builder.ToTable("TenantRoles");
        builder.HasKey(cr => cr.Id);
        builder.Property(cr => cr.Name).HasMaxLength(100).IsRequired();
        builder.Property(cr => cr.Description).HasMaxLength(500);
        builder.HasIndex(cr => new { cr.Name, cr.TenantId }).IsUnique();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(cr => cr.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserTenantRoleConfiguration : IEntityTypeConfiguration<UserTenantRole>
{
    public void Configure(EntityTypeBuilder<UserTenantRole> builder)
    {
        builder.ToTable("UserTenantRoles");
        builder.HasKey(ucr => new { ucr.UserId, ucr.TenantRoleId });
        builder.Property(ucr => ucr.UserId).HasMaxLength(450);
        builder.HasOne<AppIdentityUser>().WithMany().HasForeignKey(ucr => ucr.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<TenantRole>().WithMany().HasForeignKey(ucr => ucr.TenantRoleId).OnDelete(DeleteBehavior.Restrict);
    }
}
