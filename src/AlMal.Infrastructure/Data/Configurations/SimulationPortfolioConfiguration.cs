using AlMal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMal.Infrastructure.Data.Configurations;

public class SimulationPortfolioConfiguration : IEntityTypeConfiguration<SimulationPortfolio>
{
    public void Configure(EntityTypeBuilder<SimulationPortfolio> builder)
    {
        builder.ToTable("SimulationPortfolios");
        builder.HasKey(sp => sp.Id);

        builder.Property(sp => sp.InitialCapital).HasPrecision(18, 3);
        builder.Property(sp => sp.CashBalance).HasPrecision(18, 3);

        builder.HasIndex(sp => sp.UserId).IsUnique();

        builder.HasOne(sp => sp.User)
            .WithOne(u => u.SimulationPortfolio)
            .HasForeignKey<SimulationPortfolio>(sp => sp.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
