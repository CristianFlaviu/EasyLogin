using EasyLogin.Domain.Entities;
using EasyLogin.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyLogin.Infrastructure.Persistence.Configurations;

public class CompanyRoleConfiguration : IEntityTypeConfiguration<CompanyRole>
{
    public void Configure(EntityTypeBuilder<CompanyRole> builder)
    {
        builder.ToTable("CompanyRoles");
        builder.HasKey(cr => cr.Id);
        builder.Property(cr => cr.Name).HasMaxLength(100).IsRequired();
        builder.Property(cr => cr.Description).HasMaxLength(500);
        builder.HasIndex(cr => new { cr.Name, cr.CompanyId }).IsUnique();
        builder.HasOne<Company>().WithMany().HasForeignKey(cr => cr.CompanyId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserCompanyRoleConfiguration : IEntityTypeConfiguration<UserCompanyRole>
{
    public void Configure(EntityTypeBuilder<UserCompanyRole> builder)
    {
        builder.ToTable("UserCompanyRoles");
        builder.HasKey(ucr => new { ucr.UserId, ucr.CompanyRoleId });
        builder.Property(ucr => ucr.UserId).HasMaxLength(450);
        builder.HasOne<AppIdentityUser>().WithMany().HasForeignKey(ucr => ucr.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<CompanyRole>().WithMany().HasForeignKey(ucr => ucr.CompanyRoleId).OnDelete(DeleteBehavior.Restrict);
    }
}
