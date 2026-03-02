using AlMal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMal.Infrastructure.Data.Configurations;

public class AlertHistoryConfiguration : IEntityTypeConfiguration<AlertHistory>
{
    public void Configure(EntityTypeBuilder<AlertHistory> builder)
    {
        builder.ToTable("AlertHistories");
        builder.HasKey(ah => ah.Id);

        builder.Property(ah => ah.Message).HasMaxLength(1000).IsRequired();

        builder.HasOne(ah => ah.Alert)
            .WithMany(a => a.AlertHistories)
            .HasForeignKey(ah => ah.AlertId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
