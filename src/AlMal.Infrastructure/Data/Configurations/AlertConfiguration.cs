using AlMal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMal.Infrastructure.Data.Configurations;

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("Alerts");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Condition).HasMaxLength(50).IsRequired();
        builder.Property(a => a.TargetValue).HasPrecision(18, 3);

        builder.HasOne(a => a.User)
            .WithMany(u => u.Alerts)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Stock)
            .WithMany()
            .HasForeignKey(a => a.StockId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
