using AlMal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMal.Infrastructure.Data.Configurations;

public class FinancialStatementConfiguration : IEntityTypeConfiguration<FinancialStatement>
{
    public void Configure(EntityTypeBuilder<FinancialStatement> builder)
    {
        builder.ToTable("FinancialStatements");
        builder.HasKey(fs => fs.Id);

        builder.Property(fs => fs.Revenue).HasPrecision(18, 3);
        builder.Property(fs => fs.NetIncome).HasPrecision(18, 3);
        builder.Property(fs => fs.TotalAssets).HasPrecision(18, 3);
        builder.Property(fs => fs.TotalEquity).HasPrecision(18, 3);
        builder.Property(fs => fs.TotalDebt).HasPrecision(18, 3);
        builder.Property(fs => fs.EPS).HasPrecision(18, 6);
        builder.Property(fs => fs.DPS).HasPrecision(18, 6);
        builder.Property(fs => fs.BookValue).HasPrecision(18, 3);

        builder.HasIndex(fs => new { fs.StockId, fs.Year, fs.Quarter })
            .IsUnique()
            .HasDatabaseName("IX_FinancialStatement_StockId_Year_Quarter");

        builder.HasOne(fs => fs.Stock)
            .WithMany(s => s.FinancialStatements)
            .HasForeignKey(fs => fs.StockId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
