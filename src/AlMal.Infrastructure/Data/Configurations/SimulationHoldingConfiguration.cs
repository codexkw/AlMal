using AlMal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMal.Infrastructure.Data.Configurations;

public class SimulationHoldingConfiguration : IEntityTypeConfiguration<SimulationHolding>
{
    public void Configure(EntityTypeBuilder<SimulationHolding> builder)
    {
        builder.ToTable("SimulationHoldings");
        builder.HasKey(sh => new { sh.PortfolioId, sh.StockId });

        builder.Property(sh => sh.AverageCost).HasPrecision(18, 3);

        builder.HasOne(sh => sh.Portfolio)
            .WithMany(sp => sp.Holdings)
            .HasForeignKey(sh => sh.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sh => sh.Stock)
            .WithMany()
            .HasForeignKey(sh => sh.StockId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
