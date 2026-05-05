using EasyLogin.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyLogin.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.EventType).HasMaxLength(64).IsRequired();

        builder.Property(a => a.ActorUserId).HasMaxLength(450);
        builder.Property(a => a.ActorEmail).HasMaxLength(256);

        builder.Property(a => a.TargetType).HasMaxLength(64);
        builder.Property(a => a.TargetId).HasMaxLength(450);
        builder.Property(a => a.TargetDisplay).HasMaxLength(256);

        builder.Property(a => a.FailureReason).HasMaxLength(256);
        builder.Property(a => a.IpAddress).HasMaxLength(64);
        builder.Property(a => a.UserAgent).HasMaxLength(1024);
        builder.Property(a => a.BrowserName).HasMaxLength(64);
        builder.Property(a => a.BrowserVersion).HasMaxLength(32);
        builder.Property(a => a.OsName).HasMaxLength(64);
        builder.Property(a => a.OsVersion).HasMaxLength(32);
        builder.Property(a => a.DeviceFamily).HasMaxLength(64);
        builder.Property(a => a.Jti).HasMaxLength(64);
        builder.Property(a => a.CorrelationId).HasMaxLength(128);
        builder.Property(a => a.MetadataJson).HasColumnType("nvarchar(max)");

        builder.HasIndex(a => a.Timestamp);
        builder.HasIndex(a => a.ActorUserId);
        builder.HasIndex(a => a.ActorEmail);
        builder.HasIndex(a => a.EventType);
        builder.HasIndex(a => new { a.TargetType, a.TargetId });
    }
}
