using AlMal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMal.Infrastructure.Data.Configurations;

public class StockConfiguration : IEntityTypeConfiguration<Stock>
{
    public void Configure(EntityTypeBuilder<Stock> builder)
    {
        builder.ToTable("Stocks");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Symbol).HasMaxLength(20).IsRequired();
        builder.Property(s => s.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(s => s.NameEn).HasMaxLength(200);
        builder.Property(s => s.ISIN).HasMaxLength(20);
        builder.Property(s => s.LogoUrl).HasMaxLength(500);
        builder.Property(s => s.DescriptionAr).HasColumnType("nvarchar(max)");
        builder.Property(s => s.MarketCap).HasPrecision(18, 3);
        builder.Property(s => s.LastPrice).HasPrecision(18, 3);
        builder.Property(s => s.DayChange).HasPrecision(18, 3);
        builder.Property(s => s.DayChangePercent).HasPrecision(8, 4);

        builder.HasIndex(s => s.Symbol).IsUnique().HasDatabaseName("IX_Stock_Symbol");
        builder.HasIndex(s => s.SectorId).HasDatabaseName("IX_Stock_SectorId");
        builder.HasIndex(s => s.IsActive).HasDatabaseName("IX_Stock_IsActive");

        builder.HasOne(s => s.Sector)
            .WithMany(se => se.Stocks)
            .HasForeignKey(s => s.SectorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
