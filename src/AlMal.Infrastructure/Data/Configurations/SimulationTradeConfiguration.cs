using AlMal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMal.Infrastructure.Data.Configurations;

public class SimulationTradeConfiguration : IEntityTypeConfiguration<SimulationTrade>
{
    public void Configure(EntityTypeBuilder<SimulationTrade> builder)
    {
        builder.ToTable("SimulationTrades");
        builder.HasKey(st => st.Id);

        builder.Property(st => st.Price).HasPrecision(18, 3);
        builder.Property(st => st.TotalValue).HasPrecision(18, 3);

        builder.HasOne(st => st.Portfolio)
            .WithMany(sp => sp.Trades)
            .HasForeignKey(st => st.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(st => st.Stock)
            .WithMany()
            .HasForeignKey(st => st.StockId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
