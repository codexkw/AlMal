using AlMal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMal.Infrastructure.Data.Configurations;

public class StockPriceConfiguration : IEntityTypeConfiguration<StockPrice>
{
    public void Configure(EntityTypeBuilder<StockPrice> builder)
    {
        builder.ToTable("StockPrices");
        builder.HasKey(sp => sp.Id);

        builder.Property(sp => sp.Open).HasPrecision(18, 3);
        builder.Property(sp => sp.High).HasPrecision(18, 3);
        builder.Property(sp => sp.Low).HasPrecision(18, 3);
        builder.Property(sp => sp.Close).HasPrecision(18, 3);
        builder.Property(sp => sp.Value).HasPrecision(18, 3);

        builder.HasIndex(sp => new { sp.StockId, sp.Date })
            .IsUnique()
            .HasDatabaseName("IX_StockPrice_StockId_Date");
        builder.HasIndex(sp => sp.Date).HasDatabaseName("IX_StockPrice_Date");

        builder.HasOne(sp => sp.Stock)
            .WithMany(s => s.StockPrices)
            .HasForeignKey(sp => sp.StockId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
